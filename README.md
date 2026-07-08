# Order Tracking System

Real-time food-delivery tracking platform built with .NET 8 and React. The repository is developed incrementally, one phase per branch.

## Current phase

`phase/02-use-cases` adds CQRS use cases, repositories, REST endpoints, JWT security, optimistic concurrency and indexed geospatial queries.

## Requirements

- .NET SDK 8
- SQL Server (required when running migrations or the API against a database)

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

## Branch workflow

- `main`: stable releases.
- `develop`: integration branch.
- `phase/02-use-cases`: current implementation branch.

Each phase is validated and merged into `develop` before the next phase branch is created.
