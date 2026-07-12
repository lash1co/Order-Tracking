# Deployment readiness

Phase 6 provides local container and Kubernetes-ready assets. Public certificate provisioning and a real production rollout remain intentionally out of scope for now.

## Docker Compose

Build and run the full local stack:

```powershell
Copy-Item .env.example .env
./scripts/deployment/compose-up.ps1 -Build
```

Services:

- UI: `http://localhost:5173`
- API: `http://localhost:7247`
- SQL Server: `localhost,1433`
- Redis: `localhost:6379`
- RabbitMQ management: `http://localhost:15672`

The API exposes `/health/live` and `/health/ready`.

The dashboard requires a bearer token because the API validates JWTs. For local tutorial usage, generate one with:

```powershell
./scripts/development/create-demo-token.ps1
```

Paste the token into the React dashboard: `Configurar conexión` > `Token bearer`.

In Docker Compose, the API applies EF Core migrations automatically on startup through:

```text
Database__ApplyMigrationsOnStartup=true
```

Stop the stack:

```powershell
./scripts/deployment/compose-down.ps1
```

Remove containers and database volumes:

```powershell
./scripts/deployment/compose-down.ps1 -RemoveVolumes
```

Outside Docker Compose, database migrations are still explicit:

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

For kind, load locally built images into the cluster first:

```powershell
./scripts/deployment/build-images.ps1 -Tag local
./scripts/deployment/load-kind-images.ps1 -ClusterName kind -Tag local
```

The manifests include namespace, ConfigMap, Secret placeholders, SQL Server, Redis, RabbitMQ, API/UI Deployments, Services, probes, HPA definitions and an nginx Ingress with local TLS placeholder.

Before real cluster use, replace `order-tracking-secrets` values with a proper secret provider and provide a real TLS secret if ingress TLS is enabled.

## Helm

Helm is free and optional. Use it when you want a Kubernetes package manager experience instead of applying plain YAML.

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
