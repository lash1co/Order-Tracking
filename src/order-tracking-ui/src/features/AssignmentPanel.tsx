import { type FormEvent, useMemo, useState } from 'react';
import type { DriverLocation, Order } from '../domain/types';
import type { NearbyDriver } from '../services/apiClient';

type Props = {
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

export function AssignmentPanel({ orders, drivers, onFindNearby, onAssign }: Props) {
  const assignableOrders = useMemo(
    () => orders.filter((order) => order.status === 'Pending' || order.status === 'Preparing'),
    [orders]
  );
  const [orderId, setOrderId] = useState('');
  const [latitude, setLatitude] = useState(defaultSearch.latitude);
  const [longitude, setLongitude] = useState(defaultSearch.longitude);
  const [radiusMeters, setRadiusMeters] = useState(defaultSearch.radiusMeters);
  const [nearbyDrivers, setNearbyDrivers] = useState<NearbyDriver[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [assigningDriverId, setAssigningDriverId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selectedOrder = assignableOrders.find((order) => order.id === orderId) ?? assignableOrders[0];
  const availableVisibleDrivers = drivers.filter((driver) => driver.status === 'Available').length;

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
      if (result.length === 0) setError('No hay drivers disponibles dentro del radio seleccionado.');
    } catch {
      setError('La búsqueda falló. Revisa el toast para ver si fue token, permisos o validación.');
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
    } catch {
      setError('La asignación falló. Revisa el toast para ver si fue permisos o conflicto de asignación.');
    } finally {
      setAssigningDriverId(null);
    }
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
          <p>Busca drivers disponibles por geolocalización y asigna uno a una orden Pending/Preparing.</p>
        </div>
        <span className="pill">{availableVisibleDrivers} drivers disponibles visibles</span>
      </div>

      <form className="entity-form assignment-form" onSubmit={(event) => void search(event)}>
        <div className="form-grid assignment-grid">
          <label>
            Orden
            <select disabled={assignableOrders.length === 0} value={selectedOrder?.id ?? ''} onChange={(event) => setOrderId(event.target.value)}>
              {assignableOrders.length === 0 ? (
                <option value="">Sin órdenes asignables</option>
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

      {error && <p className="inline-error form-error">{error}</p>}

      <div className="nearby-driver-list">
        {nearbyDrivers.length === 0 ? (
          <p>Busca drivers cercanos para ver candidatos disponibles.</p>
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

function validateSearch(latitude: string, longitude: string, radiusMeters: string) {
  const parsedLatitude = Number(latitude);
  const parsedLongitude = Number(longitude);
  const parsedRadius = Number(radiusMeters);
  if (!Number.isFinite(parsedLatitude) || parsedLatitude < -90 || parsedLatitude > 90) return 'La latitud debe estar entre -90 y 90.';
  if (!Number.isFinite(parsedLongitude) || parsedLongitude < -180 || parsedLongitude > 180) return 'La longitud debe estar entre -180 y 180.';
  if (!Number.isFinite(parsedRadius) || parsedRadius <= 0 || parsedRadius > 50000) return 'El radio debe estar entre 1 y 50000 metros.';
  return null;
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('es-CO', {
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}
