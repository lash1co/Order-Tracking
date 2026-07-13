import type { DriverLocation, Order, OrderStatus } from '../domain/types';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export type DemoTokenResponse = {
  token: string;
  roles: string[];
  expiresAt: string;
};

export async function createDemoToken(roles: string[], subject: string, signal?: AbortSignal): Promise<DemoTokenResponse> {
  const response = await fetch(`${apiBaseUrl}/api/v1/dev/tokens`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ roles, subject, expiresInHours: 8 }),
    signal
  });
  return readJson<DemoTokenResponse>(response);
}

export async function getActiveOrders(token: string | null, signal?: AbortSignal): Promise<Order[]> {
  const response = await fetch(`${apiBaseUrl}/api/v1/orders/active?page=1&pageSize=100`, {
    headers: buildHeaders(token),
    signal
  });
  return readJson<Order[]>(response);
}

export type CreateOrderItemRequest = {
  menuItemId: string;
  quantity: number;
  price: number;
};

export type CreateOrderRequest = {
  customerId: string;
  restaurantId: string;
  estimatedDelivery: string;
  items: CreateOrderItemRequest[];
};

export type VehicleType = 'Bicycle' | 'Motorcycle' | 'Car';

export type CreateDriverRequest = {
  name: string;
  vehicleType: VehicleType;
  latitude: number;
  longitude: number;
};

export type CreateDriverResponse = {
  id: string;
};

export type NearbyDriver = {
  id: string;
  name: string;
  vehicleType: VehicleType;
  status: string;
  latitude: number;
  longitude: number;
  distanceMeters: number;
};

export async function createDriver(request: CreateDriverRequest, token: string | null, signal?: AbortSignal): Promise<DriverLocation> {
  const response = await fetch(`${apiBaseUrl}/api/v1/drivers`, {
    method: 'POST',
    headers: {
      ...buildHeaders(token),
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(request),
    signal
  });
  const created = await readJson<CreateDriverResponse>(response);
  return {
    driverId: created.id,
    name: request.name,
    vehicleType: request.vehicleType,
    status: 'Available',
    latitude: request.latitude,
    longitude: request.longitude,
    updatedAt: new Date().toISOString()
  };
}

export async function updateDriverLocation(
  driver: DriverLocation,
  latitude: number,
  longitude: number,
  token: string | null,
  signal?: AbortSignal
): Promise<DriverLocation> {
  const response = await fetch(`${apiBaseUrl}/api/v1/drivers/${driver.driverId}/location`, {
    method: 'PATCH',
    headers: {
      ...buildHeaders(token),
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ latitude, longitude }),
    signal
  });

  if (!response.ok) {
    const body = await response.text();
    throw new ApiError(response.status, body || `Request failed with ${response.status}`);
  }

  return {
    ...driver,
    latitude,
    longitude,
    updatedAt: new Date().toISOString()
  };
}

export async function getNearbyDrivers(
  latitude: number,
  longitude: number,
  token: string | null,
  radiusMeters = 5000,
  take = 10,
  signal?: AbortSignal
): Promise<NearbyDriver[]> {
  const parameters = new URLSearchParams({
    latitude: latitude.toString(),
    longitude: longitude.toString(),
    radiusMeters: radiusMeters.toString(),
    take: take.toString()
  });
  const response = await fetch(`${apiBaseUrl}/api/v1/drivers/nearby?${parameters.toString()}`, {
    headers: buildHeaders(token),
    signal
  });
  return readJson<NearbyDriver[]>(response);
}

export async function assignDriverToOrder(orderId: string, driverId: string, token: string | null, signal?: AbortSignal): Promise<string> {
  const response = await fetch(`${apiBaseUrl}/api/v1/orders/${orderId}/assignments`, {
    method: 'POST',
    headers: {
      ...buildHeaders(token),
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ driverId }),
    signal
  });
  const result = await readJson<{ assignmentId: string }>(response);
  return result.assignmentId;
}

export async function createOrder(request: CreateOrderRequest, token: string | null, signal?: AbortSignal): Promise<Order> {
  const response = await fetch(`${apiBaseUrl}/api/v1/orders`, {
    method: 'POST',
    headers: {
      ...buildHeaders(token),
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(request),
    signal
  });
  return readJson<Order>(response);
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
    throw new ApiError(response.status, body || `Request failed with ${response.status}`);
  }

  return response.json() as Promise<T>;
}
