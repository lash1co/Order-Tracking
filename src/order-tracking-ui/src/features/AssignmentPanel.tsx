import { type FormEvent, useMemo, useState } from 'react';
import type { AuthInfo, DriverLocation, Order } from '../domain/types';
import type { NearbyDriver } from '../services/apiClient';
import { hasAnyRole } from '../services/authToken';

type Props = {
  auth: AuthInfo;
  orders: Order[];
  drivers: DriverLocation[];
  onFindNearby(latitude: number, longitude: number, radiusMeters?: number): Promise<NearbyDriver[]>;
  onAssign(orderId: string, driver: NearbyDriver): Promise<void>;
};

const defaultSearch = {
  latitude: '4.711000',
  longitude: '-74.072100',
  radiusMeters: '5000'
};

export function AssignmentPanel({ auth, orders, drivers, onFindNearby, onAssign }: Props) {
  const assignableOrders = useMemo(
    () =>
      orders.filter(
        (order) => (order.status === 'Pending' || order.status === 'Preparing') && !order.hasActiveDriverAssignment
      ),
    [orders]
  );
  const visibleAvailableDrivers = useMemo(
    () => drivers.filter((driver) => driver.status === 'Available').sort((left, right) => left.name.localeCompare(right.name)),
    [drivers]
  );
  const [orderId, setOrderId] = useState('');
  const [manualDriverId, setManualDriverId] = useState('');
  const [latitude, setLatitude] = useState(defaultSearch.latitude);
  const [longitude, setLongitude] = useState(defaultSearch.longitude);
  const [radiusMeters, setRadiusMeters] = useState(defaultSearch.radiusMeters);
  const [nearbyDrivers, setNearbyDrivers] = useState<NearbyDriver[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [assigningDriverId, setAssigningDriverId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selectedOrder = assignableOrders.find((order) => order.id === orderId) ?? assignableOrders[0];
  const selectedManualDriver = visibleAvailableDrivers.find((driver) => driver.driverId === manualDriverId) ?? visibleAvailableDrivers[0];
  const canAssignByRole = hasAnyRole(auth, ['Admin', 'Dispatcher']);

  async function search(event: FormEvent) {
    event.preventDefault();
    setError(null);

    const validationError = validateSearch(latitude, longitude, radiusMeters);
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSearching(true);
    try {
      const result = await onFindNearby(Number(latitude), Number(longitude), Number(radiusMeters));
      setNearbyDrivers(result);
      if (result.length === 0) setError('No hay drivers disponibles dentro del radio seleccionado. Usa la asignación manual si tienes drivers visibles.');
    } catch {
      setError('La búsqueda falló. Puedes asignar manualmente un driver visible mientras revisas el servicio de cercanía.');
    } finally {
      setIsSearching(false);
    }
  }

  async function assign(driver: NearbyDriver) {
    setError(null);
    if (!selectedOrder) {
      setError('No hay órdenes pendientes o en preparación para asignar.');
      return;
    }

    setAssigningDriverId(driver.id);
    try {
      await onAssign(selectedOrder.id, driver);
      setNearbyDrivers((current) => current.filter((candidate) => candidate.id !== driver.id));
      if (manualDriverId === driver.id) setManualDriverId('');
    } catch {
      setError('La asignación falló. Revisa el toast para ver si fue permisos o conflicto de asignación.');
    } finally {
      setAssigningDriverId(null);
    }
  }

  async function assignManual() {
    if (!selectedManualDriver) {
      setError('No hay drivers disponibles visibles para asignar manualmente.');
      return;
    }

    await assign(toNearbyDriver(selectedManualDriver, Number(latitude), Number(longitude)));
  }

  function useDriverCoordinates(driver: DriverLocation) {
    setLatitude(driver.latitude.toFixed(6));
    setLongitude(driver.longitude.toFixed(6));
  }

  return (
    <section className="panel assignment-panel">
      <div className="panel-header">
        <div>
          <span className="eyebrow">Dispatch command</span>
          <h2>Asignar driver a orden</h2>
          <p>Busca drivers cercanos o asigna manualmente un driver visible disponible.</p>
        </div>
        <span className="pill">{visibleAvailableDrivers.length} drivers disponibles visibles</span>
      </div>

      <form className="entity-form assignment-form" onSubmit={(event) => void search(event)}>
        {!canAssignByRole && (
          <p className="permission-hint">Asignar drivers requiere Admin o Dispatcher. Puedes intentarlo para ver el `403` real.</p>
        )}
        <div className="form-grid assignment-grid">
          <label>
            Orden
            <select disabled={assignableOrders.length === 0} value={selectedOrder?.id ?? ''} onChange={(event) => setOrderId(event.target.value)}>
              {assignableOrders.length === 0 ? (
                <option value="">Sin órdenes pendientes/preparando sin driver</option>
              ) : (
                assignableOrders.map((order) => (
                  <option key={order.id} value={order.id}>
                    #{order.id.slice(0, 8)} · {order.status} · ETA {formatTime(order.estimatedDelivery)}
                  </option>
                ))
              )}
            </select>
          </label>
          <label>
            Latitud búsqueda
            <input type="number" step="0.000001" value={latitude} onChange={(event) => setLatitude(event.target.value)} />
          </label>
          <label>
            Longitud búsqueda
            <input type="number" step="0.000001" value={longitude} onChange={(event) => setLongitude(event.target.value)} />
          </label>
          <label>
            Radio metros
            <input type="number" min="100" max="50000" step="100" value={radiusMeters} onChange={(event) => setRadiusMeters(event.target.value)} />
          </label>
        </div>

        <div className="button-row wrap-row">
          <button type="submit" disabled={isSearching || assignableOrders.length === 0}>
            {isSearching ? 'Buscando...' : 'Buscar drivers cercanos'}
          </button>
          {drivers.slice(0, 3).map((driver) => (
            <button className="secondary-button" type="button" key={driver.driverId} onClick={() => useDriverCoordinates(driver)}>
              Usar coords {driver.name}
            </button>
          ))}
        </div>
      </form>

      <div className="manual-assignment">
        <label>
          Asignación manual
          <select
            disabled={visibleAvailableDrivers.length === 0}
            value={selectedManualDriver?.driverId ?? ''}
            onChange={(event) => setManualDriverId(event.target.value)}
          >
            {visibleAvailableDrivers.length === 0 ? (
              <option value="">Sin drivers disponibles visibles</option>
            ) : (
              visibleAvailableDrivers.map((driver) => (
                <option key={driver.driverId} value={driver.driverId}>
                  {driver.name} · {driver.vehicleType} · {driver.driverId.slice(0, 8)}
                </option>
              ))
            )}
          </select>
        </label>
        <button type="button" disabled={!selectedOrder || !selectedManualDriver || assigningDriverId !== null} onClick={() => void assignManual()}>
          {assigningDriverId === selectedManualDriver?.driverId ? 'Asignando...' : 'Asignar driver visible'}
        </button>
      </div>

      {error && <p className="inline-error form-error">{error}</p>}

      <div className="nearby-driver-list">
        {nearbyDrivers.length === 0 ? (
          <p>Busca drivers cercanos o usa la asignación manual con un driver visible.</p>
        ) : (
          nearbyDrivers.map((driver) => (
            <article className="nearby-driver-row" key={driver.id}>
              <div>
                <strong>{driver.name}</strong>
                <small>
                  {driver.vehicleType} · {driver.status} · {Math.round(driver.distanceMeters)}m
                </small>
              </div>
              <span>
                {driver.latitude.toFixed(5)}, {driver.longitude.toFixed(5)}
              </span>
              <button type="button" disabled={assigningDriverId !== null} onClick={() => void assign(driver)}>
                {assigningDriverId === driver.id ? 'Asignando...' : 'Asignar'}
              </button>
            </article>
          ))
        )}
      </div>
    </section>
  );
}

function toNearbyDriver(driver: DriverLocation, originLatitude: number, originLongitude: number): NearbyDriver {
  return {
    id: driver.driverId,
    name: driver.name,
    vehicleType: driver.vehicleType as NearbyDriver['vehicleType'],
    status: driver.status,
    latitude: driver.latitude,
    longitude: driver.longitude,
    distanceMeters: calculateDistanceMeters(originLatitude, originLongitude, driver.latitude, driver.longitude)
  };
}

function validateSearch(latitude: string, longitude: string, radiusMeters: string) {
  const parsedLatitude = Number(latitude);
  const parsedLongitude = Number(longitude);
  const parsedRadius = Number(radiusMeters);
  if (!Number.isFinite(parsedLatitude) || parsedLatitude < -90 || parsedLatitude > 90) return 'La latitud debe estar entre -90 y 90.';
  if (!Number.isFinite(parsedLongitude) || parsedLongitude < -180 || parsedLongitude > 180) return 'La longitud debe estar entre -180 y 180.';
  if (!Number.isFinite(parsedRadius) || parsedRadius <= 0 || parsedRadius > 50000) return 'El radio debe estar entre 1 y 50000 metros.';
  return null;
}

function calculateDistanceMeters(originLatitude: number, originLongitude: number, targetLatitude: number, targetLongitude: number) {
  const earthRadiusMeters = 6_371_000;
  const originLatRadians = toRadians(originLatitude);
  const targetLatRadians = toRadians(targetLatitude);
  const deltaLat = toRadians(targetLatitude - originLatitude);
  const deltaLon = toRadians(targetLongitude - originLongitude);
  const haversine =
    Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
    Math.cos(originLatRadians) * Math.cos(targetLatRadians) * Math.sin(deltaLon / 2) * Math.sin(deltaLon / 2);
  return earthRadiusMeters * 2 * Math.atan2(Math.sqrt(haversine), Math.sqrt(1 - haversine));
}

function toRadians(degrees: number) {
  return (degrees * Math.PI) / 180;
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('es-CO', {
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}
