import { type FormEvent, useMemo, useState } from 'react';
import type { DriverLocation, Order, OrderStatus } from '../domain/types';
import type { DriverPerformance } from '../services/apiClient';

type Props = {
  orders: Order[];
  drivers: DriverLocation[];
  onGetDriverPerformance(driverId: string): Promise<DriverPerformance>;
};

const orderStatuses: OrderStatus[] = ['Pending', 'Preparing', 'OutForDelivery', 'Delivered', 'Cancelled'];

export function OperationalMetricsPanel({ orders, drivers, onGetDriverPerformance }: Props) {
  const [selectedDriverId, setSelectedDriverId] = useState('');
  const [performance, setPerformance] = useState<DriverPerformance | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sortedDrivers = useMemo(
    () => [...drivers].sort((left, right) => left.name.localeCompare(right.name)),
    [drivers]
  );
  const selectedDriver = sortedDrivers.find((driver) => driver.driverId === selectedDriverId) ?? sortedDrivers[0];
  const ordersByStatus = useMemo(() => countBy(orders, (order) => order.status), [orders]);
  const driversByStatus = useMemo(() => countBy(drivers, (driver) => driver.status), [drivers]);
  const etaRisk = orders.filter((order) => order.status !== 'Delivered' && new Date(order.estimatedDelivery).getTime() < Date.now()).length;
  const avgItems = orders.length === 0 ? 0 : orders.reduce((sum, order) => sum + order.items.length, 0) / orders.length;

  async function loadPerformance(event: FormEvent) {
    event.preventDefault();
    setError(null);

    if (!selectedDriver) {
      setError('No hay drivers visibles todavía. Crea uno o espera eventos del simulador.');
      return;
    }

    setIsLoading(true);
    try {
      setPerformance(await onGetDriverPerformance(selectedDriver.driverId));
    } catch {
      setError('No se pudo consultar la performance. Revisa el toast para ver el detalle.');
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="panel metrics-panel">
      <div className="panel-header">
        <div>
          <span className="eyebrow">Operational metrics</span>
          <h2>Métricas y performance</h2>
          <p>Lee el estado operativo cargado en la UI y consulta performance real por driver desde la API.</p>
        </div>
        <span className={`pill ${etaRisk > 0 ? 'warn-pill' : ''}`}>{etaRisk} ETA en riesgo</span>
      </div>

      <div className="metrics-grid">
        <article className="metric-card">
          <h3>Órdenes por estado</h3>
          <div className="metric-bars">
            {orderStatuses.map((status) => (
              <MetricBar key={status} label={status} value={ordersByStatus[status] ?? 0} max={Math.max(orders.length, 1)} />
            ))}
          </div>
        </article>

        <article className="metric-card">
          <h3>Drivers por estado</h3>
          <div className="metric-bars">
            {Object.entries(driversByStatus).length === 0 ? (
              <p className="muted-text">Sin drivers visibles todavía.</p>
            ) : (
              Object.entries(driversByStatus).map(([status, value]) => (
                <MetricBar key={status} label={status} value={value} max={Math.max(drivers.length, 1)} />
              ))
            )}
          </div>
        </article>

        <article className="metric-card">
          <h3>Resumen cargado</h3>
          <dl className="metric-definition-list">
            <div>
              <dt>Órdenes cargadas</dt>
              <dd>{orders.length}</dd>
            </div>
            <div>
              <dt>Drivers visibles</dt>
              <dd>{drivers.length}</dd>
            </div>
            <div>
              <dt>Items promedio</dt>
              <dd>{avgItems.toFixed(1)}</dd>
            </div>
          </dl>
        </article>

        <article className="metric-card">
          <h3>Performance driver</h3>
          <form className="performance-form" onSubmit={(event) => void loadPerformance(event)}>
            <label>
              Driver
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
            <button type="submit" disabled={isLoading || sortedDrivers.length === 0}>
              {isLoading ? 'Consultando...' : 'Consultar performance'}
            </button>
          </form>

          {performance && (
            <dl className="metric-definition-list performance-result">
              <div>
                <dt>Entregas completadas</dt>
                <dd>{performance.completedDeliveries}</dd>
              </div>
              <div>
                <dt>Promedio entrega</dt>
                <dd>{performance.averageDeliveryMinutes.toFixed(1)} min</dd>
              </div>
            </dl>
          )}
        </article>
      </div>

      {error && <p className="inline-error form-error">{error}</p>}
    </section>
  );
}

function MetricBar({ label, value, max }: { label: string; value: number; max: number }) {
  const percentage = Math.min(100, Math.round((value / max) * 100));

  return (
    <div className="metric-bar">
      <div>
        <span>{label}</span>
        <strong>{value}</strong>
      </div>
      <div className="metric-bar-track">
        <span style={{ width: `${percentage}%` }} />
      </div>
    </div>
  );
}

function countBy<T>(items: T[], selector: (item: T) => string) {
  return items.reduce<Record<string, number>>((acc, item) => {
    const key = selector(item);
    acc[key] = (acc[key] ?? 0) + 1;
    return acc;
  }, {});
}
