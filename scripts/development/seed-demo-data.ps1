param(
    [string]$ApiBaseUrl = "http://localhost:7247",
    [string]$Token = "",
    [switch]$SkipStatusTransitions
)

$ErrorActionPreference = "Stop"

function Get-DemoToken {
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        return $Token
    }

    $tokenOutput = & "$PSScriptRoot/create-demo-token.ps1" -Roles Admin, Dispatcher, Driver -Subject "demo-seed-admin"
    $jwt = $tokenOutput |
        Where-Object { $_ -match "^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$" } |
        Select-Object -Last 1

    if ([string]::IsNullOrWhiteSpace($jwt)) {
        throw "Could not generate a demo JWT. Check JWT_SIGNING_KEY in .env or .env.example."
    }

    return $jwt
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null
    )

    $headers = @{
        Authorization = "Bearer $script:Jwt"
    }

    $uri = "$ApiBaseUrl$Path"
    if ($null -eq $Body) {
        return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers
    }

    $json = $Body | ConvertTo-Json -Depth 10
    return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -ContentType "application/json" -Body $json
}

function New-DemoOrderBody {
    param(
        [int]$Index,
        [int]$EtaMinutes
    )

    $items = @(
        @{
            menuItemId = [guid]::NewGuid()
            quantity = 1 + ($Index % 3)
            price = 9.90 + $Index
        },
        @{
            menuItemId = [guid]::NewGuid()
            quantity = 1
            price = 4.50 + ($Index / 2)
        }
    )

    return @{
        customerId = [guid]::NewGuid()
        restaurantId = [guid]::NewGuid()
        estimatedDelivery = [DateTimeOffset]::UtcNow.AddMinutes($EtaMinutes).ToString("O")
        items = $items
    }
}

Write-Host ""
Write-Host "Seeding demo data against $ApiBaseUrl ..." -ForegroundColor Cyan

$script:Jwt = Get-DemoToken

try {
    Invoke-Api -Method Get -Path "/health/live" | Out-Null
}
catch {
    throw "API is not reachable at $ApiBaseUrl. Start Docker Compose first: ./scripts/deployment/compose-up.ps1 -Build"
}

$driversToCreate = @(
    @{ name = "Ana Moto"; vehicleType = "Motorcycle"; latitude = 4.7110; longitude = -74.0721 },
    @{ name = "Luis Bike"; vehicleType = "Bicycle"; latitude = 4.7045; longitude = -74.0648 },
    @{ name = "Marta Car"; vehicleType = "Car"; latitude = 4.7212; longitude = -74.0815 },
    @{ name = "Carlos Moto"; vehicleType = "Motorcycle"; latitude = 4.6992; longitude = -74.0750 }
)

$drivers = @()
foreach ($driverBody in $driversToCreate) {
    $driver = Invoke-Api -Method Post -Path "/api/v1/drivers" -Body $driverBody
    $drivers += [pscustomobject]@{
        Id = $driver.id
        Name = $driverBody.name
        VehicleType = $driverBody.vehicleType
    }
    Write-Host "Created driver $($driverBody.name): $($driver.id)"
}

$orders = @()
$etaValues = @(20, 30, 45, 60, 75)
for ($i = 0; $i -lt $etaValues.Count; $i++) {
    $order = Invoke-Api -Method Post -Path "/api/v1/orders" -Body (New-DemoOrderBody -Index ($i + 1) -EtaMinutes $etaValues[$i])
    $orders += $order
    Write-Host "Created order #$($order.id.ToString().Substring(0, 8)) with status $($order.status)"
}

if (-not $SkipStatusTransitions) {
    $firstOrder = $orders[0]
    $secondOrder = $orders[1]
    $thirdOrder = $orders[2]

    $firstOrder = Invoke-Api -Method Patch -Path "/api/v1/orders/$($firstOrder.id)/status" -Body @{
        status = "Preparing"
        version = $firstOrder.version
    }

    Invoke-Api -Method Post -Path "/api/v1/orders/$($firstOrder.id)/assignments" -Body @{
        driverId = $drivers[0].Id
    } | Out-Null

    $firstOrder = Invoke-Api -Method Get -Path "/api/v1/orders/$($firstOrder.id)"
    $firstOrder = Invoke-Api -Method Patch -Path "/api/v1/orders/$($firstOrder.id)/status" -Body @{
        status = "OutForDelivery"
        version = $firstOrder.version
    }
    Write-Host "Moved order #$($firstOrder.id.ToString().Substring(0, 8)) to $($firstOrder.status)"

    $secondOrder = Invoke-Api -Method Patch -Path "/api/v1/orders/$($secondOrder.id)/status" -Body @{
        status = "Preparing"
        version = $secondOrder.version
    }
    Write-Host "Moved order #$($secondOrder.id.ToString().Substring(0, 8)) to $($secondOrder.status)"

    Invoke-Api -Method Post -Path "/api/v1/orders/$($thirdOrder.id)/assignments" -Body @{
        driverId = $drivers[1].Id
    } | Out-Null
    Write-Host "Assigned driver $($drivers[1].Name) to order #$($thirdOrder.id.ToString().Substring(0, 8))"
}

Write-Host ""
Write-Host "Demo data ready." -ForegroundColor Green
Write-Host "Open the UI at http://localhost:5173, paste a demo token, and watch the dashboard update."
Write-Host ""
