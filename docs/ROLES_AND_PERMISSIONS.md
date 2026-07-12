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

To prove authorization is working, generate a Driver token and call an Admin/Dispatcher-only endpoint such as:

```http
POST /api/v1/orders
```

The expected result is:

```text
403 Forbidden
```
