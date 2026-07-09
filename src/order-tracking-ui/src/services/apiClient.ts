import type { Order, OrderStatus } from '../domain/types';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

export async function getActiveOrders(token: string | null, signal?: AbortSignal): Promise<Order[]> {
  const response = await fetch(`${apiBaseUrl}/api/v1/orders/active?page=1&pageSize=100`, {
    headers: buildHeaders(token),
    signal
  });
  return readJson<Order[]>(response);
}

export async function updateOrderStatus(
  order: Order,
  status: OrderStatus,
  token: string | null,
  signal?: AbortSignal
): Promise<Order> {
  const response = await fetch(`${apiBaseUrl}/api/v1/orders/${order.id}/status`, {
    method: 'PATCH',
    headers: {
      ...buildHeaders(token),
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ status, version: order.version }),
    signal
  });
  return readJson<Order>(response);
}

function buildHeaders(token: string | null): HeadersInit {
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.text();
    throw new Error(body || `Request failed with ${response.status}`);
  }

  return response.json() as Promise<T>;
}
