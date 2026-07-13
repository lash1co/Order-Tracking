# Roles and permissions

The project uses JWT bearer authentication. A production system would normally issue tokens from an identity provider. For this learning repository, local scripts generate signed demo tokens using the same `JWT_SIGNING_KEY` configured for the API.

## Demo tokens

In Docker Compose, the React dashboard can request demo tokens from:

```text
POST /api/v1/dev/tokens
```

This endpoint is controlled by `DemoTokens:Enabled`. It is enabled in local Docker Compose and disabled by default in `appsettings.json`.

You can also generate a token with all tutorial roles from PowerShell:

```powershell
./scripts/development/create-demo-token.ps1
```

Generate role-specific tokens:

```powershell
./scripts/development/create-demo-token.ps1 -Roles Admin -Subject admin-demo
./scripts/development/create-demo-token.ps1 -Roles Dispatcher -Subject dispatcher-demo
./scripts/development/create-demo-token.ps1 -Roles Driver -Subject driver-demo
```

## Permission matrix

| Capability | Admin | Dispatcher | Driver |
|---|---:|---:|---:|
| Load active orders | Yes | Yes | Yes |
| Connect to SignalR tracking hub | Yes | Yes | Yes |
| Create order | Yes | Yes | No |
| Change order status | Yes | Yes | Yes |
| Create driver | Yes | Yes | No |
| Assign driver to order | Yes | Yes | No |
| Update driver location | Yes | No | Yes |
| Query nearby drivers | Yes | Yes | Yes |
| Query driver performance | Yes | Yes | Yes |

Driver performance is read-only. The delivered-count metric increases when an assigned order reaches `Delivered`, because the active assignment is completed and the driver is released back to `Available`.

## UI permission feedback

The React dashboard includes a `Sesión actual` panel. It decodes the JWT payload locally to show the subject, active roles, expiration and a tutorial permission matrix.

This does not replace backend authorization. It only explains what should happen before the user clicks a button. The protected API endpoints still enforce roles and return:

- `401 Unauthorized` when the token is missing, invalid or expired;
- `403 Forbidden` when the token is valid but the role is not allowed;
- `409 Conflict` when an order/driver assignment or optimistic version is no longer valid;
- `429 Too Many Requests` when the local rate limit is exceeded.

## Tutorial scenarios

### Admin

Use Admin when you want to prepare the demo system:

1. create drivers;
2. create orders;
3. assign drivers;
4. inspect dashboard behavior;
5. advance order status.

### Dispatcher

Use Dispatcher when you want to simulate the operations team:

1. create orders;
2. find nearby drivers;
3. assign a driver;
4. move orders from `Pending` to `Preparing` or `OutForDelivery`.

### Driver

Use Driver when you want to simulate delivery execution:

1. update driver location;
2. advance an assigned order;
3. mark an order as `Delivered`.

### Permission failure

To prove authorization is working from the UI:

1. Open `Configurar conexión`.
2. Choose `Usar Driver`.
3. Try to create an order from the `Crear orden` panel.

The expected result is a permission error and no new order.

You can also choose `Usar Dispatcher` and try to update a driver location from `Administrar drivers`. The expected result is also `403 Forbidden`, because only `Admin` and `Driver` can update locations.

Finally, choose `Usar Driver` and try to assign a driver from `Asignar driver a orden`. The expected result is `403 Forbidden`, because assignment is an Admin/Dispatcher operation.

You can also call an Admin/Dispatcher-only endpoint such as:

```http
POST /api/v1/orders
```

The expected result is:

```text
403 Forbidden
```
