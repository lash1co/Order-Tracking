# Order Tracking System

Real-time food-delivery tracking platform built with .NET 8 and React. The repository is developed incrementally, one phase per branch.

## Current phase

`phase/01-foundation` establishes Clean Architecture, the domain model, EF Core persistence, error handling and unit tests.

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

## Branch workflow

- `main`: stable releases.
- `develop`: integration branch.
- `phase/01-foundation`: current implementation branch.

Each phase is validated and merged into `develop` before the next phase branch is created.
