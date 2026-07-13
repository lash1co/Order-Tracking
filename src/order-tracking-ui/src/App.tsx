import { useMemo, useState } from 'react';
import { AssignmentPanel } from './features/AssignmentPanel';
import { ConnectionBanner } from './features/ConnectionBanner';
import { CreateOrderPanel } from './features/CreateOrderPanel';
import { DashboardMap } from './features/DashboardMap';
import { DemoRolePanel } from './features/DemoRolePanel';
import { DriverAdminPanel } from './features/DriverAdminPanel';
import { KpiCards } from './features/KpiCards';
import { OrderList } from './features/OrderList';
import { OperationalMetricsPanel } from './features/OperationalMetricsPanel';
import { ToastRegion } from './features/ToastRegion';
import { TokenForm } from './features/TokenForm';
import { useDashboard } from './state/DashboardContext';

export function App() {
  const { state, actions } = useDashboard();
  const [showSettings, setShowSettings] = useState(false);
  const subtitle = useMemo(() => {
    if (state.connection.status === 'connected') return 'Actualizaciones en vivo activas';
    if (state.connection.status === 'reconnecting') return 'Reconectando y preparando reconciliación';
    return 'Modo lectura inicial / conexión pendiente';
  }, [state.connection.status]);

  return (
    <main className="app-shell">
      <section className="hero-card">
        <div>
          <span className="eyebrow">Operations dashboard</span>
          <h1>Order Tracking System</h1>
          <p>{subtitle}</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => setShowSettings((value) => !value)}>
          {showSettings ? 'Ocultar conexión' : 'Configurar conexión'}
        </button>
      </section>

      {showSettings && (
        <>
          <DemoRolePanel onToken={actions.setAuthToken} />
          <TokenForm token={state.authToken} onSave={actions.setAuthToken} />
        </>
      )}

      <ConnectionBanner connection={state.connection} onReconnect={actions.reconnectAndSync} />
      <CreateOrderPanel onCreate={actions.createOrder} />
      <DriverAdminPanel drivers={state.drivers} onCreate={actions.createDriver} onUpdateLocation={actions.updateDriverLocation} />
      <AssignmentPanel
        orders={state.orders}
        drivers={state.drivers}
        onFindNearby={actions.findNearbyDrivers}
        onAssign={actions.assignDriver}
      />
      <OperationalMetricsPanel orders={state.orders} drivers={state.drivers} onGetDriverPerformance={actions.getDriverPerformance} />
      <KpiCards orders={state.orders} drivers={state.drivers} />

      <section className="dashboard-grid">
        <OrderList orders={state.orders} onOptimisticStatus={actions.optimisticStatusUpdate} />
        <DashboardMap drivers={state.drivers} />
      </section>

      <ToastRegion toasts={state.toasts} onDismiss={actions.dismissToast} />
    </main>
  );
}
