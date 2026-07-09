param(
    [string]$ClusterName = "kind",
    [string]$Tag = "local"
)

$ErrorActionPreference = "Stop"

kind load docker-image "order-tracking-api:$Tag" --name $ClusterName
kind load docker-image "order-tracking-ui:$Tag" --name $ClusterName
