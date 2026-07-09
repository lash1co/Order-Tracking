# Deployment readiness

Phase 6 provides local container and Kubernetes-ready assets. Public certificate provisioning and a real production rollout remain intentionally out of scope for now.

## Docker Compose

Build and run the full local stack:

```powershell
$env:JWT_SIGNING_KEY = "local-development-signing-key-32-characters-minimum"
$env:SQLSERVER_SA_PASSWORD = "Your_strong_password123!"
docker compose up --build
```

Services:

- UI: `http://localhost:5173`
- API: `http://localhost:7247`
- SQL Server: `localhost,1433`
- Redis: `localhost:6379`
- RabbitMQ management: `http://localhost:15672`

The API exposes `/health/live` and `/health/ready`.

Database migrations are still explicit:

```powershell
dotnet ef database update `
  --project src/OrderTracking.Infrastructure `
  --startup-project src/OrderTracking.API
```

## Docker images

```powershell
./scripts/deployment/build-images.ps1 -Tag local
```

Generated images:

- `order-tracking-api:local`
- `order-tracking-ui:local`

## Kubernetes manifests

Apply the plain manifests:

```powershell
./scripts/deployment/apply-k8s.ps1
```

The manifests include namespace, ConfigMap, Secret placeholders, SQL Server, Redis, RabbitMQ, API/UI Deployments, Services, probes, HPA definitions and an nginx Ingress with local TLS placeholder.

Before real cluster use, replace `order-tracking-secrets` values with a proper secret provider and provide a real TLS secret if ingress TLS is enabled.

## Helm

Render locally:

```powershell
helm template order-tracking ./kubernetes/helm-chart --namespace order-tracking
```

Install or upgrade:

```powershell
helm upgrade --install order-tracking ./kubernetes/helm-chart `
  --namespace order-tracking `
  --create-namespace
```

## Observability

The API uses Serilog structured console logging and OpenTelemetry instrumentation for ASP.NET Core, HTTP client and runtime metrics. An OTLP exporter is not enabled in this phase because available exporter package versions were blocked by NuGet vulnerability policy. A collector/exporter can be added once an approved package version is selected.
