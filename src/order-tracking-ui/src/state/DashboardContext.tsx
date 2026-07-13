import React, { createContext, useCallback, useContext, useEffect, useMemo, useReducer, useRef } from 'react';
import type { ConnectionStatus, DriverLocation, Order, OrderStatus, Toast } from '../domain/types';
import { ApiError, createOrder, getActiveOrders, updateOrderStatus, type CreateOrderRequest } from '../services/apiClient';
import { TrackingHubClient } from '../services/trackingHub';

type DashboardState = {
  authToken: string | null;
  orders: Order[];
  drivers: DriverLocation[];
  connection: {
    status: ConnectionStatus;
    lastSyncAt?: string;
    connectionId?: string | null;
    error?: string;
  };
  toasts: Toast[];
};

type DashboardActions = {
  setAuthToken(token: string | null): void;
  reconnectAndSync(): Promise<void>;
  createOrder(request: CreateOrderRequest): Promise<void>;
  optimisticStatusUpdate(order: Order, status: OrderStatus): Promise<void>;
  dismissToast(id: string): void;
};

type DashboardContextValue = {
  state: DashboardState;
  actions: DashboardActions;
};

type Action =
  | { type: 'token.set'; token: string | null }
  | { type: 'orders.synced'; orders: Order[]; syncedAt: string }
  | { type: 'order.changed'; order: Order }
  | { type: 'driver.changed'; driver: DriverLocation }
  | { type: 'connection.changed'; status: ConnectionStatus; connectionId?: string | null; error?: string }
  | { type: 'toast.add'; toast: Toast }
  | { type: 'toast.dismiss'; id: string };

const DashboardContext = createContext<DashboardContextValue | null>(null);

const initialState: DashboardState = {
  authToken: localStorage.getItem('orderTracking.authToken'),
  orders: [],
  drivers: [],
  connection: { status: 'disconnected' },
  toasts: []
};

export function DashboardProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(reducer, initialState);
  const stateRef = useRef(state);
  const hubRef = useRef<TrackingHubClient | null>(null);
  stateRef.current = state;

  const syncOrders = useCallback(async () => {
    const controller = new AbortController();
    const orders = await getActiveOrders(stateRef.current.authToken, controller.signal);
    dispatch({ type: 'orders.synced', orders, syncedAt: new Date().toISOString() });
  }, []);

  const reconnectAndSync = useCallback(async () => {
    await syncOrders();
    await hubRef.current?.disconnect();
    hubRef.current = createHubClient(() => stateRef.current.authToken, dispatch, syncOrders);
    await hubRef.current.connect();
  }, [syncOrders]);

  useEffect(() => {
    hubRef.current = createHubClient(() => stateRef.current.authToken, dispatch, syncOrders);
    void syncOrders().catch((error: Error) =>
      dispatch({ type: 'toast.add', toast: createToast('warning', `No se pudieron cargar órdenes: ${error.message}`) })
    );
    void hubRef.current.connect().catch((error: Error) =>
      dispatch({ type: 'connection.changed', status: 'disconnected', error: error.message })
    );

    return () => {
      void hubRef.current?.disconnect();
    };
  }, [syncOrders]);

  const actions = useMemo<DashboardActions>(
    () => ({
      setAuthToken(token) {
        if (token) localStorage.setItem('orderTracking.authToken', token);
        else localStorage.removeItem('orderTracking.authToken');
        dispatch({ type: 'token.set', token });
        void reconnectAndSync().catch((error: Error) =>
          dispatch({ type: 'toast.add', toast: createToast('error', `Reconexión fallida: ${error.message}`) })
        );
      },
      reconnectAndSync,
      async createOrder(request) {
        try {
          const saved = await createOrder(request, stateRef.current.authToken);
          dispatch({ type: 'order.changed', order: saved });
          dispatch({ type: 'toast.add', toast: createToast('success', `Orden ${shortId(saved.id)} creada`) });
          await syncOrders();
        } catch (error) {
          dispatch({ type: 'toast.add', toast: createToast('error', friendlyApiMessage(error, 'No se pudo crear la orden.')) });
          throw error;
        }
      },
      async optimisticStatusUpdate(order, status) {
        const previous = stateRef.current.orders.find((item) => item.id === order.id);
        dispatch({ type: 'order.changed', order: { ...order, status } });

        try {
          const saved = await updateOrderStatus(order, status, stateRef.current.authToken);
          dispatch({ type: 'order.changed', order: saved });
          dispatch({ type: 'toast.add', toast: createToast('success', `Orden ${shortId(order.id)} actualizada`) });
        } catch (error) {
          if (previous) dispatch({ type: 'order.changed', order: previous });
          dispatch({
            type: 'toast.add',
            toast: createToast('error', `No se pudo actualizar la orden ${shortId(order.id)}. Reconciliando datos.`)
          });
          await syncOrders();
        }
      },
      dismissToast(id) {
        dispatch({ type: 'toast.dismiss', id });
      }
    }),
    [reconnectAndSync, syncOrders]
  );

  return <DashboardContext.Provider value={{ state, actions }}>{children}</DashboardContext.Provider>;
}

function friendlyApiMessage(error: unknown, fallback: string) {
  if (error instanceof ApiError) {
    if (error.status === 401) return 'No autorizado: el token falta o expiró. Elige un rol demo y reintenta.';
    if (error.status === 403) return 'Permiso denegado: este rol no puede ejecutar esa acción.';
    if (error.status === 409) return 'Conflicto de datos: sincroniza el dashboard y reintenta.';
    if (error.status === 429) return 'Demasiadas solicitudes: espera un momento y reintenta.';
  }

  return fallback;
}

export function useDashboard() {
  const value = useContext(DashboardContext);
  if (!value) throw new Error('useDashboard must be used inside DashboardProvider.');
  return value;
}

function reducer(state: DashboardState, action: Action): DashboardState {
  switch (action.type) {
    case 'token.set':
      return { ...state, authToken: action.token };
    case 'orders.synced':
      return { ...state, orders: action.orders, connection: { ...state.connection, lastSyncAt: action.syncedAt } };
    case 'order.changed':
      return { ...state, orders: upsertBy(state.orders, action.order, (order) => order.id) };
    case 'driver.changed':
      return { ...state, drivers: upsertBy(state.drivers, action.driver, (driver) => driver.driverId) };
    case 'connection.changed':
      return {
        ...state,
        connection: {
          ...state.connection,
          status: action.status,
          connectionId: action.connectionId,
          error: action.error
        }
      };
    case 'toast.add':
      return { ...state, toasts: [action.toast, ...state.toasts].slice(0, 5) };
    case 'toast.dismiss':
      return { ...state, toasts: state.toasts.filter((toast) => toast.id !== action.id) };
  }
}

function createHubClient(getToken: () => string | null, dispatch: React.Dispatch<Action>, syncOrders: () => Promise<void>) {
  return new TrackingHubClient(getToken, {
    onOrderChanged(order) {
      dispatch({ type: 'order.changed', order });
      dispatch({ type: 'toast.add', toast: createToast('info', `Cambio en orden ${shortId(order.id)}: ${order.status}`) });
    },
    onDriverLocationChanged(driver) {
      dispatch({ type: 'driver.changed', driver });
    },
    onConnecting() {
      dispatch({ type: 'connection.changed', status: 'connecting' });
    },
    onConnected(connectionId) {
      dispatch({ type: 'connection.changed', status: 'connected', connectionId });
      void syncOrders();
    },
    onReconnecting(error) {
      dispatch({ type: 'connection.changed', status: 'reconnecting', error: error?.message });
    },
    onDisconnected(error) {
      dispatch({ type: 'connection.changed', status: 'disconnected', error: error?.message });
    }
  });
}

function upsertBy<T>(items: T[], next: T, keySelector: (item: T) => string): T[] {
  const key = keySelector(next);
  const index = items.findIndex((item) => keySelector(item) === key);
  if (index === -1) return [next, ...items];
  return items.map((item, itemIndex) => (itemIndex === index ? next : item));
}

function createToast(tone: Toast['tone'], message: string): Toast {
  return { id: crypto.randomUUID(), tone, message };
}

function shortId(id: string) {
  return id.slice(0, 8);
}
