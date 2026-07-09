# Quality and performance checks

Phase 5 adds executable quality gates for the API, dashboard and realtime edge.

## Backend

```powershell
dotnet restore
dotnet build OrderTracking.sln --no-restore
dotnet test OrderTracking.sln --no-build --collect:"XPlat Code Coverage"
```

The integration test project uses `WebApplicationFactory` for smoke checks that do not require a running SQL Server:

- `/health/live` returns `200 OK`.
- protected REST endpoints reject anonymous traffic.
- the SignalR hub rejects anonymous HTTP access.

Database-backed integration suites with Testcontainers are intentionally deferred until the Docker phase, where SQL Server, Redis and RabbitMQ are orchestrated together.

## Frontend

```powershell
pnpm install --config.confirmModulesPurge=false
pnpm --filter order-tracking-ui typecheck
pnpm --filter order-tracking-ui build
```

E2E smoke tests use Playwright and mocked API responses:

```powershell
pnpm --filter order-tracking-ui exec playwright install chromium
pnpm --filter order-tracking-ui test:e2e
```

The smoke checks verify that dashboard KPIs render and optimistic status transitions are reflected in the UI.

## k6 performance smoke

Short load smoke for active orders:

```powershell
k6 run -e BASE_URL=https://localhost:7247 -e TOKEN="<jwt>" scripts/performance/order-tracking-load.js
```

SignalR negotiate smoke:

```powershell
k6 run -e BASE_URL=https://localhost:7247 -e TOKEN="<jwt>" scripts/performance/signalr-negotiate-smoke.js
```

These scripts are intentionally bounded and are not the excluded 24-hour stress run. They are meant for repeatable local or CI checks before the DevOps/container phase.
