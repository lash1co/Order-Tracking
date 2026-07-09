# HTTP API v1

Base path: `/api/v1`. All business endpoints require a JWT bearer token. Roles used by write operations are `Admin`, `Dispatcher` and `Driver`.

## Orders

- `POST /orders` creates an order. Roles: Admin or Dispatcher.
- `GET /orders/{id}` returns an order and its current version in both the response body and `ETag`.
- `GET /orders/active?page=1&pageSize=50` returns active orders, newest first. Maximum page size: 200.
- `PATCH /orders/{id}/status` advances the state using the Base64 `version` supplied by the latest read. A stale version returns `409 Conflict`.
- `POST /orders/{id}/assignments` assigns an available driver. Roles: Admin or Dispatcher.

## Drivers

- `POST /drivers` registers an available driver. Roles: Admin or Dispatcher.
- `PATCH /drivers/{id}/location` updates validated coordinates. Roles: Admin or Driver.
- `GET /drivers/nearby` accepts `latitude`, `longitude`, `radiusMeters` and `take`. Radius is capped at 50 km and results at 100.
- `GET /drivers/{id}/performance` returns completed deliveries and average delivery minutes.

## SignalR hub

- Hub path: `/hubs/tracking`.
- Authentication: JWT bearer. SignalR clients may use `access_token` in the query string during the WebSocket/SSE negotiation.
- On connect, clients are added to the `dashboard` group automatically. They can also call `SubscribeDashboard` and `UnsubscribeDashboard`.

Server-to-client events:

- `order.changed`: emits the latest `OrderDto` after create/status changes.
- `driver.location.changed`: emits driver id, name, vehicle type, status, latitude, longitude and update timestamp.

## React dashboard integration

The React app calls:

- `GET /api/v1/orders/active?page=1&pageSize=100` during initial load and after reconnects.
- `PATCH /api/v1/orders/{id}/status` for optimistic status transitions.
- `/hubs/tracking` for live reconciliation events.

The dashboard stores a manually supplied bearer token in `localStorage` under `orderTracking.authToken` for local demo convenience. Production identity integration is intentionally deferred.

## Errors and limits

Errors use `application/problem+json`. Business validation returns 400, missing resources 404, concurrency conflicts 409 and unexpected errors 500. Requests are limited to 100 per minute per authenticated user or source IP.

## Authentication configuration

Required settings:

- `Jwt:SigningKey`: at least 32 bytes, supplied through environment variables or a secret store.
- `Jwt:Issuer`
- `Jwt:Audience`

Swagger exposes bearer authentication during development. This service validates tokens but does not issue them.
