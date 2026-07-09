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

export type Toast = {
  id: string;
  tone: 'info' | 'success' | 'warning' | 'error';
  message: string;
};
