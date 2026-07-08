# Order Tracking System

Real-time food-delivery tracking platform built with .NET 8 and React. The repository is developed incrementally, one phase per branch.

## Current phase

`phase/03-realtime-events` adds SignalR live updates, optional Redis active-order caching, optional RabbitMQ integration events and a configurable background driver movement simulator.

## Requirements

- .NET SDK 8
- SQL Server (required when running migrations or the API against a database)
- Redis and RabbitMQ are optional in this phase. They are disabled by default and can be enabled through configuration.

## Build and test

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

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

The default connection string is intended only for local development. Override it with `ConnectionStrings__OrderTracking` in real environments.

The API deliberately fails fast when no secure JWT key is configured. For local use, set a value of at least 32 bytes without committing it:

```powershell
$env:Jwt__SigningKey = "your-local-signing-key-at-least-32-bytes"
dotnet run --project src/OrderTracking.API
```

Swagger UI is available at `/swagger` in Development. Token issuance is expected to be handled by an external identity provider; this API validates issuer, audience, signature and lifetime.

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
- `phase/03-realtime-events`: current implementation branch.

Each phase is validated and merged into `develop` before the next phase branch is created.
