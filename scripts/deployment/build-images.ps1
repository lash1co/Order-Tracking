param(
    [string]$Tag = "local"
)

$ErrorActionPreference = "Stop"

docker build `
    --file src/OrderTracking.API/Dockerfile `
    --tag "order-tracking-api:$Tag" `
    .

docker build `
    --file src/order-tracking-ui/Dockerfile `
    --tag "order-tracking-ui:$Tag" `
    .
