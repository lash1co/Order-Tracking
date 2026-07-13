import { type FormEvent, useMemo, useState } from 'react';
import type { AuthInfo, DriverLocation } from '../domain/types';
import type { CreateDriverRequest, VehicleType } from '../services/apiClient';
import { hasAnyRole } from '../services/authToken';

type Props = {
  auth: AuthInfo;
  drivers: DriverLocation[];
  onCreate(request: CreateDriverRequest): Promise<void>;
  onUpdateLocation(driver: DriverLocation, latitude: number, longitude: number): Promise<void>;
};

const vehicleTypes: VehicleType[] = ['Bicycle', 'Motorcycle', 'Car'];

const bogotaCenter = {
  latitude: 4.711,
  longitude: -74.0721
};

export function DriverAdminPanel({ auth, drivers, onCreate, onUpdateLocation }: Props) {
  const [name, setName] = useState('Demo Driver');
  const [vehicleType, setVehicleType] = useState<VehicleType>('Motorcycle');
  const [latitude, setLatitude] = useState(bogotaCenter.latitude.toString());
  const [longitude, setLongitude] = useState(bogotaCenter.longitude.toString());
  const [selectedDriverId, setSelectedDriverId] = useState('');
  const [nextLatitude, setNextLatitude] = useState((bogotaCenter.latitude + 0.002).toFixed(6));
  const [nextLongitude, setNextLongitude] = useState((bogotaCenter.longitude - 0.002).toFixed(6));
  const [isCreating, setIsCreating] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sortedDrivers = useMemo(
    () => [...drivers].sort((left, right) => left.name.localeCompare(right.name)),
    [drivers]
  );
  const selectedDriver = sortedDrivers.find((driver) => driver.driverId === selectedDriverId) ?? sortedDrivers[0];
  const canCreateByRole = hasAnyRole(auth, ['Admin', 'Dispatcher']);
  const canUpdateByRole = hasAnyRole(auth, ['Admin', 'Driver']);

  async function submitCreate(event: FormEvent) {
    event.preventDefault();
    setError(null);

    const validationError = validateDriver(name, latitude, longitude);
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsCreating(true);
    try {
      await onCreate({
        name: name.trim(),
        vehicleType,
        latitude: Number(latitude),
        longitude: Number(longitude)
      });
      setName(`Demo Driver ${Math.floor(Math.random() * 1000)}`);
      nudgeCreateCoordinates();
    } catch {
      setError('La API rechazó la creación. Revisa el toast para ver si fue token, permisos o validación.');
    } finally {
      setIsCreating(false);
    }
  }

  async function submitLocation(event: FormEvent) {
    event.preventDefault();
    setError(null);

    if (!selectedDriver) {
      setError('Primero crea o espera a que llegue un driver por SignalR.');
      return;
    }

    const validationError = validateCoordinates(nextLatitude, nextLongitude);
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsUpdating(true);
    try {
      await onUpdateLocation(selectedDriver, Number(nextLatitude), Number(nextLongitude));
      setNextLatitude((Number(nextLatitude) + 0.001).toFixed(6));
      setNextLongitude((Number(nextLongitude) - 0.001).toFixed(6));
    } catch {
      setError('La API rechazó la ubicación. Revisa el toast para ver si fue token, permisos o validación.');
    } finally {
      setIsUpdating(false);
    }
  }

  function nudgeCreateCoordinates() {
    setLatitude((Number(latitude) + 0.0015).toFixed(6));
    setLongitude((Number(longitude) - 0.0015).toFixed(6));
  }

  return (
    <section className="panel driver-admin-panel">
      <div className="panel-header">
        <div>
          <span className="eyebrow">Driver command</span>
          <h2>Administrar drivers</h2>
          <p>Crea drivers como Admin/Dispatcher y actualiza ubicación como Admin/Driver.</p>
        </div>
        <span className="pill">{drivers.length} visibles</span>
      </div>

      <div className="driver-admin-grid">
        <form className="entity-form compact-form" onSubmit={(event) => void submitCreate(event)}>
          <h3>Crear driver</h3>
          {!canCreateByRole && <p className="permission-hint">Crear drivers requiere Admin o Dispatcher. Inténtalo para ver el `403` real.</p>}
          <label>
            Nombre
            <input value={name} onChange={(event) => setName(event.target.value)} />
          </label>
          <label>
            Vehículo
            <select value={vehicleType} onChange={(event) => setVehicleType(event.target.value as VehicleType)}>
              {vehicleTypes.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>
          <div className="form-grid two-columns">
            <label>
              Latitud
              <input type="number" step="0.000001" value={latitude} onChange={(event) => setLatitude(event.target.value)} />
            </label>
            <label>
              Longitud
              <input type="number" step="0.000001" value={longitude} onChange={(event) => setLongitude(event.target.value)} />
            </label>
          </div>
          <button type="submit" disabled={isCreating}>
            {isCreating ? 'Creando...' : 'Crear driver'}
          </button>
        </form>

        <form className="entity-form compact-form" onSubmit={(event) => void submitLocation(event)}>
          <h3>Actualizar ubicación</h3>
          {!canUpdateByRole && <p className="permission-hint">Actualizar ubicación requiere Admin o Driver. Inténtalo para ver el `403` real.</p>}
          <label>
            Driver visible
            <select
              disabled={sortedDrivers.length === 0}
              value={selectedDriver?.driverId ?? ''}
              onChange={(event) => setSelectedDriverId(event.target.value)}
            >
              {sortedDrivers.length === 0 ? (
                <option value="">Sin drivers visibles</option>
              ) : (
                sortedDrivers.map((driver) => (
                  <option key={driver.driverId} value={driver.driverId}>
                    {driver.name} · {driver.status}
                  </option>
                ))
              )}
            </select>
          </label>
          <div className="form-grid two-columns">
            <label>
              Nueva latitud
              <input type="number" step="0.000001" value={nextLatitude} onChange={(event) => setNextLatitude(event.target.value)} />
            </label>
            <label>
              Nueva longitud
              <input type="number" step="0.000001" value={nextLongitude} onChange={(event) => setNextLongitude(event.target.value)} />
            </label>
          </div>
          <button type="submit" disabled={isUpdating || sortedDrivers.length === 0}>
            {isUpdating ? 'Actualizando...' : 'Actualizar ubicación'}
          </button>
        </form>
      </div>

      {error && <p className="inline-error form-error">{error}</p>}

      <div className="driver-list">
        {sortedDrivers.length === 0 ? (
          <p>No hay drivers visibles todavía. Crea uno o espera eventos del simulador.</p>
        ) : (
          sortedDrivers.slice(0, 8).map((driver) => (
            <article className="driver-list-row" key={driver.driverId}>
              <div>
                <strong>{driver.name}</strong>
                <small>
                  {driver.vehicleType} · {driver.status} · {driver.driverId.slice(0, 8)}
                </small>
              </div>
              <span>
                {driver.latitude.toFixed(5)}, {driver.longitude.toFixed(5)}
              </span>
            </article>
          ))
        )}
      </div>
    </section>
  );
}

function validateDriver(name: string, latitude: string, longitude: string) {
  if (name.trim().length < 2) return 'El nombre debe tener al menos 2 caracteres.';
  return validateCoordinates(latitude, longitude);
}

function validateCoordinates(latitude: string, longitude: string) {
  const parsedLatitude = Number(latitude);
  const parsedLongitude = Number(longitude);
  if (!Number.isFinite(parsedLatitude) || parsedLatitude < -90 || parsedLatitude > 90) return 'La latitud debe estar entre -90 y 90.';
  if (!Number.isFinite(parsedLongitude) || parsedLongitude < -180 || parsedLongitude > 180) return 'La longitud debe estar entre -180 y 180.';
  return null;
}
