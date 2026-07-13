# End-to-end use cases

This document is the guided product tutorial for the Order Tracking System. Follow it after the stack is running and demo data has been created.

## Prerequisites

Start from the repository root:

```powershell
Copy-Item .env.example .env
./scripts/deployment/compose-up.ps1 -Build
./scripts/development/seed-demo-data.ps1
```

Open:

```text
http://localhost:5173
```

Then click `Configurar conexión` and use the role buttons in the tutorial panel.

## Use case 1: Admin prepares the system

Actor: Admin.

Goal: create operational data and verify the dashboard reacts.

Steps:

1. Click `Configurar conexión`.
2. Choose `Usar Admin`.
3. Confirm `Sesión actual` shows the `Admin` role.
4. In `Crear orden`, create a new order.
5. In `Administrar drivers`, create a new driver.
6. Confirm the order list, KPIs and map update.

Expected result:

- the order appears as `Pending`;
- the driver appears as `Available`;
- toasts confirm successful commands;
- another browser tab receives updates through SignalR.

Technical path demonstrated:

- React command form;
- JWT role authorization;
- CQRS command handler;
- EF Core persistence;
- SignalR notification;
- Redis active-order cache invalidation;
- RabbitMQ event publishing.

## Use case 2: Dispatcher assigns a driver

Actor: Dispatcher.

Goal: assign an available driver to an operational order.

Steps:

1. Choose `Usar Dispatcher`.
2. Make sure there is at least one `Pending` or `Preparing` order.
3. Make sure there is at least one `Available` driver.
4. In `Asignar driver a orden`, select an order.
5. Use default Bogotá coordinates or click `Usar coords <driver>`.
6. Click `Buscar drivers cercanos`.
7. Pick a nearby driver and click `Asignar`.

Expected result:

- the driver changes to `Assigned`;
- the map updates;
- a success toast appears;
- the order can now move to `OutForDelivery`.

Technical path demonstrated:

- SQL Server geography query;
- assignment conflict prevention;
- role-based authorization;
- driver status change;
- real-time map update.

## Use case 3: Driver completes a delivery

Actor: Driver.

Goal: simulate execution of a delivery.

Steps:

1. Choose `Usar Driver`.
2. In `Administrar drivers`, select a visible driver.
3. Update its latitude/longitude.
4. In the order list, advance an assigned order through:
   - `Preparing`;
   - `OutForDelivery`;
   - `Delivered`.

Expected result:

- location update is sent through SignalR;
- when the order reaches `OutForDelivery`, the driver becomes `Delivering`;
- when the order reaches `Delivered`, the assignment is completed and the driver becomes `Available`;
- the order performance history is updated.

Technical path demonstrated:

- driver role permissions;
- order status state machine;
- assignment completion;
- domain invariants;
- driver performance history.

## Use case 4: Operations watches the live dashboard

Actor: operations viewer using any valid tutorial role.

Goal: observe the system without refreshing the page.

Steps:

1. Open the UI in two browser tabs.
2. Use a valid role token in both tabs.
3. In tab A, create an order, assign a driver or update a driver location.
4. Watch tab B.

Expected result:

- tab B receives updates automatically;
- toast notifications explain changes;
- the connection banner remains `Conectado`;
- KPIs and metrics update from the local UI state.

Technical path demonstrated:

- SignalR hub;
- WebSocket/SSE/long-polling fallback configuration;
- reconnect-and-sync behavior;
- optimistic UI updates.

## Use case 5: Permission failure

Actor: Driver.

Goal: prove that UI hints do not replace backend authorization.

Steps:

1. Choose `Usar Driver`.
2. Confirm `Sesión actual` shows `Driver`.
3. Try to create an order.
4. Try to assign a driver.

Expected result:

- the command panels warn that the role is not expected to have permission;
- the backend returns `403 Forbidden`;
- the UI shows a permission-related toast;
- no unauthorized change is persisted.

Technical path demonstrated:

- JWT role claims;
- ASP.NET Core `[Authorize(Roles = ...)]`;
- clear UI error handling.

## Use case 6: Conflict and reconciliation

Actor: Admin or Dispatcher.

Goal: demonstrate robust conflict handling.

Steps:

1. Assign a driver to an order.
2. Try assigning the same driver or same order again.
3. Optionally open two tabs and advance the same order concurrently.

Expected result:

- duplicate assignment returns `409 Conflict`;
- stale order versions are rejected;
- UI shows an error toast and syncs data again.

Technical path demonstrated:

- assignment conflict checks;
- optimistic concurrency through row versions;
- data reconciliation after failed optimistic updates.

## Use case 7: Metrics and performance

Actor: Admin or Dispatcher.

Goal: verify analytics-like behavior after delivery.

Steps:

1. Assign a driver to an order.
2. Advance the order to `OutForDelivery`.
3. Advance it to `Delivered`.
4. In `Métricas y performance`, select that driver.
5. Click `Consultar performance`.

Expected result:

- completed deliveries increase;
- average delivery minutes is calculated from assignment history;
- driver state returns to `Available`.

Technical path demonstrated:

- read model query;
- assignment completion;
- performance calculation from persisted data.

## Use case 8: Connection recovery

Actor: any valid role.

Goal: verify the dashboard recovers after a temporary backend interruption.

Steps:

1. Keep the UI open.
2. Restart the API container.
3. Watch the connection banner.
4. Click `Reconciliar ahora` if needed.

Expected result:

- the banner moves through disconnected/reconnecting states;
- once the API is back, orders are synchronized again;
- users get a clear connection message instead of a silent failure.

Technical path demonstrated:

- SignalR automatic reconnect;
- explicit reconciliation;
- friendly connection errors.

## Recommended demo script for reviewers

For a concise walkthrough, use this sequence:

1. Start stack and seed data.
2. Use Admin to create one order and one driver.
3. Use Dispatcher to assign the driver.
4. Use Driver to update location and deliver the order.
5. Use Admin to check performance metrics.
6. Use Driver to intentionally trigger `403` by creating an order.
7. Open a second tab and repeat one update to demonstrate real-time sync.

This covers the main full-stack story without needing external services beyond Docker Compose.
