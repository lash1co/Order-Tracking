# Order Tracking System

Real-time food-delivery tracking platform built with .NET 8 and React. The repository is designed so reviewers can run it locally with Docker Compose, or inspect/deploy the Kubernetes assets when they want the advanced path.

## Status

The planned non-extra scope is complete on `develop`: backend, realtime, React dashboard, integration quality gates, Docker, Docker Compose, Kubernetes manifests, Helm chart, deployment scripts, health probes and structured observability basics.

Explicitly excluded for now: WebRTC, ML.NET, React Native, public certificate automation and 24-hour continuous stress tests.

## Choose your run mode

### 1. Quick start: Docker Compose

Recommended for most people reviewing the repository:

```powershell
Copy-Item .env.example .env
./scripts/deployment/compose-up.ps1 -Build
```

Then open:

- UI: `http://localhost:5173`
- API health: `http://localhost:7247/health/live`
- RabbitMQ management: `http://localhost:15672`

Generate a demo JWT and paste it in the dashboard under `Configurar conexión`:

```powershell
./scripts/development/create-demo-token.ps1
```

This repository intentionally does not include a real identity provider. The script signs a local learning token with the same `JWT_SIGNING_KEY` used by Docker Compose.

Docker Compose also enables a local tutorial endpoint for the React dashboard, so you can click `Configurar conexión` and choose `Usar Admin`, `Usar Dispatcher` or `Usar Driver` without manually pasting a JWT.

Create tutorial data through the API:

```powershell
./scripts/development/seed-demo-data.ps1
```

The seed script creates sample drivers, orders and assignments so the React dashboard immediately shows live order rows, KPIs and map updates.

The dashboard also includes a `Crear orden` panel. Use `Usar Admin` or `Usar Dispatcher` in the demo role panel to create orders from the UI; use `Usar Driver` to demonstrate the expected `403 Forbidden` permission path.

The `Administrar drivers` panel lets `Admin` or `Dispatcher` create drivers and lets `Admin` or `Driver` update driver locations. The map and visible driver list update as location events arrive.

Stop it with:

```powershell
./scripts/deployment/compose-down.ps1
```

To remove database volumes too:

```powershell
./scripts/deployment/compose-down.ps1 -RemoveVolumes
```

### 2. Developer mode

Use this when changing code without containers:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
pnpm install --config.confirmModulesPurge=false
pnpm --filter order-tracking-ui dev
```

### 3. Kubernetes local or advanced review

Use this if you have Docker Desktop Kubernetes, minikube, kind, k3d or Rancher Desktop:

```powershell
./scripts/deployment/build-images.ps1 -Tag local
./scripts/deployment/apply-k8s.ps1
```

For kind clusters, load local images first:

```powershell
./scripts/deployment/load-kind-images.ps1 -ClusterName kind -Tag local
```

Helm is optional:

```powershell
helm upgrade --install order-tracking ./kubernetes/helm-chart `
  --namespace order-tracking `
  --create-namespace
```

## Requirements

- .NET SDK 8
- Node.js 22+ and pnpm 11+
- Docker Desktop or compatible Docker engine for the easiest full-stack run.
- SQL Server, Redis and RabbitMQ only if running services outside Docker Compose.

## Build and test

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Build the React dashboard:

```powershell
pnpm install --config.confirmModulesPurge=false
pnpm --filter order-tracking-ui build
```

Run the dashboard locally in developer mode:

```powershell
pnpm --filter order-tracking-ui dev
```

By default, Vite proxies `/api` and `/hubs` to `https://localhost:7247`. Set `VITE_API_BASE_URL` in `src/order-tracking-ui/.env.local` only when the API is hosted elsewhere.

Quality gates and performance smoke scripts are documented in `docs/QUALITY.md`. Deployment assets are documented in `docs/DEPLOYMENT.md`.
The local end-to-end tutorial is documented in `docs/LOCAL_TESTING.md`, and role behavior is documented in `docs/ROLES_AND_PERMISSIONS.md`.

Create or update a local SQL Server database with:

```powershell
dotnet ef database update `
  --project src/OrderTracking.Infrastructure `
  --startup-project src/OrderTracking.API
```

## Projects

- `OrderTracking.Domain`: business entities, value objects and invariants.
- `OrderTracking.Application`: use-case abstractions and persistence ports.
- `OrderTracking.Infrastructure`: EF Core and SQL Server adapters.
- `OrderTracking.API`: HTTP composition and cross-cutting error handling.
- `OrderTracking.UnitTests`: domain behavior tests.
- `OrderTracking.IntegrationTests`: API smoke tests with `WebApplicationFactory`.
- `order-tracking-ui`: React dashboard built with Vite, SignalR, Leaflet and react-window.

The default connection string is intended only for local development. Override it with `ConnectionStrings__OrderTracking` in real environments.

The API deliberately fails fast when no secure JWT key is configured. For local use, set a value of at least 32 bytes without committing it:

```powershell
$env:Jwt__SigningKey = "your-local-signing-key-at-least-32-bytes"
dotnet run --project src/OrderTracking.API
```

Swagger UI is available at `/swagger` in Development. Token issuance is expected to be handled by an external identity provider; this API validates issuer, audience, signature and lifetime.

For local learning/demo runs, generate a compatible JWT with:

```powershell
./scripts/development/create-demo-token.ps1
```

Paste the output token into the React dashboard connection panel. The generated token includes the tutorial roles `Admin`, `Dispatcher` and `Driver`.

## Real-time and eventing configuration

SignalR is exposed at `/hubs/tracking`. Browser clients can pass the bearer token through the normal `Authorization` header or the `access_token` query parameter used by SignalR transports.

Optional runtime features:

```powershell
$env:Redis__Enabled = "true"
$env:Redis__Configuration = "localhost:6379"
$env:RabbitMQ__Enabled = "true"
$env:RabbitMQ__HostName = "localhost"
$env:DriverMovementSimulator__Enabled = "true"
```

When Redis or RabbitMQ are disabled, the application uses no-op adapters so local API development remains simple. In Development, the driver simulator is enabled by default and retries on transient database failures.

## Branch workflow

- `main`: stable releases.
- `develop`: integration branch.
- `phase/*`: completed implementation branches kept for traceability.

Each phase is validated and merged into `develop` before the next phase branch is created.
