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

## 3. Generate a token for the UI

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

## 4. Try each role

Generate role-specific tokens:

```powershell
./scripts/development/create-demo-token.ps1 -Roles Admin -Subject admin-demo
./scripts/development/create-demo-token.ps1 -Roles Dispatcher -Subject dispatcher-demo
./scripts/development/create-demo-token.ps1 -Roles Driver -Subject driver-demo
```

Paste each token in the UI connection panel and try the dashboard again.

Current UI support:

- all roles can load the dashboard and connect to SignalR;
- `Admin`, `Dispatcher` and `Driver` can advance order statuses from the list;
- creation of orders, creation of drivers and driver assignment are currently tested through the API or seed script.

## 5. Real-time browser test

1. Open the dashboard in two browser tabs.
2. Paste the same valid token in both tabs.
3. Advance an order status in tab A.
4. Watch tab B receive the update and toast without refreshing.

You can also restart the API container and observe the connection banner moving through disconnected/reconnecting states before syncing again.

## 6. API role examples

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

## 7. What this tutorial proves

- Clean Architecture flow from controller to application handler to EF Core repository.
- JWT authentication and role authorization.
- Optimistic concurrency through order row versions.
- Real-time updates with SignalR and reconnect/sync behavior.
- Driver movement simulation.
- Redis-backed active order caching.
- RabbitMQ event publishing and analytics consumer.
- Docker Compose local infrastructure.

## Troubleshooting

### The UI shows 401

Generate a fresh token and paste it again:

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

