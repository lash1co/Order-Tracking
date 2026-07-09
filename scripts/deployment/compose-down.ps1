param(
    [switch]$RemoveVolumes
)

$ErrorActionPreference = "Stop"

if ($RemoveVolumes) {
    docker compose down --volumes
}
else {
    docker compose down
}
