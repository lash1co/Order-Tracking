export type OrderStatus = 'Pending' | 'Preparing' | 'OutForDelivery' | 'Delivered' | 'Cancelled';

export type OrderItem = {
  id: string;
  menuItemId: string;
  quantity: number;
  price: number;
};

export type Order = {
  id: string;
  customerId: string;
  restaurantId: string;
  status: OrderStatus;
  createdAt: string;
  estimatedDelivery: string;
  actualDelivery?: string | null;
  version: string;
  hasActiveDriverAssignment: boolean;
  items: OrderItem[];
};

export type DriverLocation = {
  driverId: string;
  name: string;
  vehicleType: string;
  status: string;
  latitude: number;
  longitude: number;
  updatedAt: string;
};

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

export type UserRole = 'Admin' | 'Dispatcher' | 'Driver';

export type AuthInfo = {
  subject?: string;
  roles: UserRole[];
  expiresAt?: string;
  isExpired: boolean;
  isValidToken: boolean;
};

export type Toast = {
  id: string;
  tone: 'info' | 'success' | 'warning' | 'error';
  message: string;
};
