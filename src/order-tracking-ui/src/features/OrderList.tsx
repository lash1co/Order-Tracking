import { FixedSizeList, type ListChildComponentProps } from 'react-window';
import type { Order, OrderStatus } from '../domain/types';

type Props = {
  orders: Order[];
  onOptimisticStatus(order: Order, status: OrderStatus): Promise<void>;
};

const nextStatus: Partial<Record<OrderStatus, OrderStatus>> = {
  Pending: 'Preparing',
  Preparing: 'OutForDelivery',
  OutForDelivery: 'Delivered'
};

export function OrderList({ orders, onOptimisticStatus }: Props) {
  return (
    <section className="panel order-panel">
      <div className="panel-header">
        <div>
          <span className="eyebrow">Virtualized feed</span>
          <h2>Órdenes en vivo</h2>
        </div>
        <span className="pill">{orders.length} registros</span>
      </div>

      {orders.length === 0 ? (
        <div className="empty-state">
          <div className="skeleton title" />
          <div className="skeleton line" />
          <p>No hay órdenes activas cargadas todavía.</p>
        </div>
      ) : (
        <FixedSizeList
          className="order-list"
          height={520}
          width="100%"
          itemCount={orders.length}
          itemSize={118}
          itemData={{ orders, onOptimisticStatus }}
        >
          {OrderRow}
        </FixedSizeList>
      )}
    </section>
  );
}

function OrderRow({ index, style, data }: ListChildComponentProps<{ orders: Order[]; onOptimisticStatus: Props['onOptimisticStatus'] }>) {
  const order = data.orders[index];
  const next = nextStatus[order.status];

  return (
    <article className="order-row" style={style}>
      <div>
        <div className="order-row-title">
          <strong>#{order.id.slice(0, 8)}</strong>
          <span className={`status-dot ${order.status}`} />
          <span>{order.status}</span>
        </div>
        <small>
          ETA:{' '}
          {new Intl.DateTimeFormat('es-CO', {
            hour: '2-digit',
            minute: '2-digit'
          }).format(new Date(order.estimatedDelivery))}
        </small>
        <small>{order.items.length} ítems · Cliente {order.customerId.slice(0, 8)}</small>
      </div>
      <button type="button" disabled={!next} onClick={() => next && void data.onOptimisticStatus(order, next)}>
        {next ? `Avanzar a ${next}` : 'Finalizada'}
      </button>
    </article>
  );
}
