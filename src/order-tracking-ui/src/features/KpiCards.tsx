import type { DriverLocation, Order, OrderStatus } from '../domain/types';

type Props = {
  orders: Order[];
  drivers: DriverLocation[];
};

export function KpiCards({ orders, drivers }: Props) {
  const delivered = orders.filter((order) => order.status === 'Delivered').length;
  const active = orders.filter((order) => !['Delivered', 'Cancelled'].includes(order.status)).length;
  const late = orders.filter((order) => new Date(order.estimatedDelivery).getTime() < Date.now()).length;
  const byStatus = countByStatus(orders);

  return (
    <section className="kpi-grid">
      <KpiCard label="Órdenes activas" value={active} detail={`${orders.length} cargadas`} />
      <KpiCard label="Drivers visibles" value={drivers.length} detail="Ubicaciones recibidas" />
      <KpiCard label="Entregadas" value={delivered} detail="Ventana actual" />
      <KpiCard label="Riesgo ETA" value={late} detail={`Preparing: ${byStatus.Preparing ?? 0}`} tone={late > 0 ? 'warn' : 'ok'} />
    </section>
  );
}

function KpiCard({ label, value, detail, tone }: { label: string; value: number; detail: string; tone?: 'ok' | 'warn' }) {
  return (
    <article className={`kpi-card ${tone ?? ''}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{detail}</small>
    </article>
  );
}

function countByStatus(orders: Order[]) {
  return orders.reduce<Partial<Record<OrderStatus, number>>>((acc, order) => {
    acc[order.status] = (acc[order.status] ?? 0) + 1;
    return acc;
  }, {});
}
