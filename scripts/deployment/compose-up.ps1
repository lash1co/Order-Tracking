param(
    [switch]$Build
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Created .env from .env.example. Review secrets before using this outside local development."
}

if ($Build) {
    docker compose up --build
}
else {
    docker compose up
}
