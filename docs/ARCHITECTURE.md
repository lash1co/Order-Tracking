# Architecture

## Dependency rule

The solution follows Clean Architecture. Dependencies point inward:

```text
API -> Application -> Domain
API -> Infrastructure -> Application
Infrastructure -> Domain
```

- **Domain** owns entities, value objects, state transitions and business exceptions. It has no external package dependency.
- **Application** defines use-case and persistence ports. CQRS handlers will be introduced in phase 2.
- **Infrastructure** implements persistence with EF Core and SQL Server.
- **API** is the composition root and translates failures into standard Problem Details responses.

## Consistency

`Order` and `Driver` use SQL Server `rowversion` columns. Update commands carry the version returned by the API. Stale versions and EF Core concurrency races return HTTP 409.

All domain dates are represented as `DateTimeOffset` and normalized to UTC at entity boundaries.

## Persistence

The initial migration creates Orders, OrderItems, Drivers and DriverAssignments with foreign keys and indexes for the primary query paths. The generated idempotent SQL script is stored under `scripts/database`.

Driver coordinates are represented by a validated domain value object and persisted as latitude/longitude columns. Infrastructure synchronizes an additional SQL Server `geography` point and the migration creates a spatial index for nearest-driver queries.

## Decisions deferred to later phases

- SignalR, Redis, RabbitMQ and transactional outbox: phase 3.
- React dashboard: phase 4.
- Integration, E2E and load tests: phase 5.
- Containers, Kubernetes and observability stack: phase 6.
