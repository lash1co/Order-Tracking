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

`Order` and `Driver` use SQL Server `rowversion` columns. Phase 2 will pass these concurrency tokens through commands and return HTTP 409 when a competing update wins.

All domain dates are represented as `DateTimeOffset` and normalized to UTC at entity boundaries.

## Persistence

The initial migration creates Orders, OrderItems, Drivers and DriverAssignments with foreign keys and indexes for the primary query paths. The generated idempotent SQL script is stored under `scripts/database`.

Driver coordinates are represented by a validated domain value object and persisted as the requested latitude/longitude columns. The indexed SQL Server geography representation and nearest-driver query belong to phase 2.

## Decisions deferred to later phases

- CQRS handlers and repositories: phase 2.
- SignalR, Redis, RabbitMQ and transactional outbox: phase 3.
- React dashboard: phase 4.
- Integration, E2E and load tests: phase 5.
- Containers, Kubernetes and observability stack: phase 6.
