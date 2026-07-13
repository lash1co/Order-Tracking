# Local testing tutorial

This guide walks through the complete local demo flow for the Order Tracking System. It is intended for people cloning the repository to learn how the backend, React dashboard, SignalR, SQL Server, Redis and RabbitMQ work together.

## 1. Start the stack

From the repository root:

```powershell
Copy-Item .env.example .env
./scripts/deployment/compose-up.ps1 -Build
```

Open these local URLs:

- React dashboard: `http://localhost:5173`
- API health: `http://localhost:7247/health/live`
- Swagger UI: `http://localhost:7247/swagger`
- RabbitMQ management: `http://localhost:15672`

If you want to start with a fresh database:

```powershell
./scripts/deployment/compose-down.ps1 -RemoveVolumes
./scripts/deployment/compose-up.ps1 -Build
```

## 2. Create demo data

Run:

```powershell
./scripts/development/seed-demo-data.ps1
```

The script calls the public API, not the database directly. That means it also validates authentication, authorization, business rules, SignalR notifications, Redis cache invalidation and RabbitMQ event publishing.

It creates:

- demo drivers around Bogotá;
- demo orders with several estimated delivery times;
- one order in `OutForDelivery`;
- one order in `Preparing`;
- additional pending orders;
- active driver assignments.

## 3. Choose a demo role in the UI

The Docker Compose profile enables a local tutorial endpoint that creates short-lived JWTs for the React dashboard.

1. Open `http://localhost:5173`.
2. Click `Configurar conexión`.
3. Choose `Usar Admin`, `Usar Dispatcher` or `Usar Driver`.
4. The dashboard will save the generated token and reconnect automatically.

Use this path when you want the simplest learning flow.

## 4. Generate a token manually

Use a full-access learning token:

```powershell
./scripts/development/create-demo-token.ps1
```

Then:

1. Open `http://localhost:5173`.
2. Click `Configurar conexión`.
3. Paste the generated token into `Token bearer`.
4. Click `Guardar y reconectar`.

The order list, KPIs and map should start showing live data.

## 5. Try each role

Generate role-specific tokens:

```powershell
./scripts/development/create-demo-token.ps1 -Roles Admin -Subject admin-demo
./scripts/development/create-demo-token.ps1 -Roles Dispatcher -Subject dispatcher-demo
./scripts/development/create-demo-token.ps1 -Roles Driver -Subject driver-demo
```

Paste each token in the UI connection panel and try the dashboard again.

Current UI support:

- all roles can load the dashboard and connect to SignalR;
- the role cards can request a local demo token when `DemoTokens:Enabled` is true;
- `Admin` and `Dispatcher` can create orders from the dashboard;
- `Admin` and `Dispatcher` can create drivers from the dashboard;
- `Admin` and `Dispatcher` can assign available drivers to pending/preparing orders;
- `Admin` and `Driver` can update driver locations from the dashboard;
- `Admin`, `Dispatcher` and `Driver` can advance order statuses from the list;

## 6. Create an order from the UI

1. Click `Configurar conexión`.
2. Choose `Usar Admin` or `Usar Dispatcher`.
3. In `Crear orden`, keep the generated demo GUIDs or replace them with valid GUIDs.
4. Adjust the estimated delivery time.
5. Add or remove items.
6. Click `Crear orden`.

Expected result:

- a success toast appears;
- the order appears in the live order list as `Pending`;
- another open browser tab receives the new order through SignalR;
- RabbitMQ receives an order event;
- the active orders cache is invalidated.

Now repeat the same flow with `Usar Driver`.

Expected result:

- the API rejects the command with `403 Forbidden`;
- the UI shows a permission-related toast;
- no order is created.

## 7. Create and move drivers from the UI

Create a driver:

1. Click `Configurar conexión`.
2. Choose `Usar Admin` or `Usar Dispatcher`.
3. In `Administrar drivers`, fill name, vehicle and coordinates.
4. Click `Crear driver`.

Expected result:

- the new driver appears in the visible drivers list;
- the driver appears on the map;
- the driver movement simulator may start moving it after a few seconds.

Update location:

1. Choose `Usar Admin` or `Usar Driver`.
2. Select a visible driver in `Actualizar ubicación`.
3. Enter new latitude/longitude.
4. Click `Actualizar ubicación`.

Expected result:

- the map marker moves;
- another browser tab receives the driver location update through SignalR.

Permission check:

1. Choose `Usar Dispatcher`.
2. Try `Actualizar ubicación`.

Expected result:

- the API rejects the request with `403 Forbidden`;
- the UI shows a permission-related toast.

## 8. Assign a driver from the UI

1. Click `Configurar conexión`.
2. Choose `Usar Admin` or `Usar Dispatcher`.
3. Make sure there is at least one `Pending` or `Preparing` order.
4. Make sure there is at least one `Available` driver. You can create one from `Administrar drivers`.
5. In `Asignar driver a orden`, choose the order.
6. Use the default Bogotá coordinates or click `Usar coords <driver name>`.
7. Click `Buscar drivers cercanos`.
8. Click `Asignar` on one of the nearby drivers.

Expected result:

- a success toast appears;
- the assigned driver changes from `Available` to `Assigned`;
- the map/list updates through SignalR;
- the order can now be advanced to `OutForDelivery` from the order list.

Permission check:

1. Choose `Usar Driver`.
2. Try to assign a driver.

Expected result:

- the API rejects the request with `403 Forbidden`;
- the UI shows a permission-related toast.

Conflict check:

1. Assign a driver to an order.
2. Try assigning the same driver or the same order again.

Expected result:

- the API rejects the request with `409 Conflict`.

## 9. Real-time browser test

1. Open the dashboard in two browser tabs.
2. Paste the same valid token in both tabs.
3. Create an order, create a driver, assign a driver, move a driver or advance an order status in tab A.
4. Watch tab B receive the update without refreshing.

You can also restart the API container and observe the connection banner moving through disconnected/reconnecting states before syncing again.

## 10. API role examples

Use Swagger or any REST client with the bearer token.

Admin and Dispatcher can create orders:

```http
POST /api/v1/orders
Authorization: Bearer <admin-or-dispatcher-token>
```

Driver cannot create orders. The expected result is `403 Forbidden`.

Driver can update locations:

```http
PATCH /api/v1/drivers/{driverId}/location
Authorization: Bearer <driver-token>
```

Driver can also advance order status:

```http
PATCH /api/v1/orders/{orderId}/status
Authorization: Bearer <driver-token>
```

Admin and Dispatcher can assign drivers:

```http
POST /api/v1/orders/{orderId}/assignments
Authorization: Bearer <admin-or-dispatcher-token>
```

Driver cannot assign drivers. The expected result is `403 Forbidden`.

## 11. What this tutorial proves

- Clean Architecture flow from controller to application handler to EF Core repository.
- JWT authentication and role authorization.
- Command execution from React forms.
- Geospatial nearby-driver queries.
- Driver assignment conflict prevention.
- Driver location updates from React to SignalR.
- Optimistic concurrency through order row versions.
- Real-time updates with SignalR and reconnect/sync behavior.
- Driver movement simulation.
- Redis-backed active order caching.
- RabbitMQ event publishing and analytics consumer.
- Docker Compose local infrastructure.

## Troubleshooting

### The UI shows 401

Click `Configurar conexión` and choose a demo role again. Or generate a fresh token manually and paste it:

```powershell
./scripts/development/create-demo-token.ps1
```

Tokens expire by default after 8 hours.

### The UI is empty

Run:

```powershell
./scripts/development/seed-demo-data.ps1
```

Then click `Guardar y reconectar` in the UI.

### The API is not reachable

Check:

```powershell
docker compose ps
```

Then open:

```text
http://localhost:7247/health/live
```

### I see duplicate demo data

The seed script intentionally creates new records each time so it is safe for demos. If you want a clean database, remove volumes:

```powershell
./scripts/deployment/compose-down.ps1 -RemoveVolumes
./scripts/deployment/compose-up.ps1 -Build
./scripts/development/seed-demo-data.ps1
```
