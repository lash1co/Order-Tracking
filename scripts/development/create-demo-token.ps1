param(
    [string[]]$Roles = @("Admin", "Dispatcher", "Driver"),
    [string]$Subject = "demo-user",
    [int]$ExpiresInHours = 8,
    [string]$Issuer = "OrderTracking.API",
    [string]$Audience = "OrderTracking.UI",
    [string]$SigningKey = ""
)

$ErrorActionPreference = "Stop"

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)
    [Convert]::ToBase64String($Bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

function ConvertFrom-PlainJson {
    param([object]$Value)
    $json = $Value | ConvertTo-Json -Compress -Depth 10
    [System.Text.Encoding]::UTF8.GetBytes($json)
}

function Read-DotEnvValue {
    param(
        [string]$Path,
        [string]$Name
    )

    if (-not (Test-Path $Path)) {
        return $null
    }

    $line = Get-Content $Path |
        Where-Object { $_ -match "^\s*$Name\s*=" } |
        Select-Object -First 1

    if (-not $line) {
        return $null
    }

    return ($line -split "=", 2)[1].Trim().Trim('"').Trim("'")
}

if ([string]::IsNullOrWhiteSpace($SigningKey)) {
    $SigningKey = Read-DotEnvValue -Path ".env" -Name "JWT_SIGNING_KEY"
}

if ([string]::IsNullOrWhiteSpace($SigningKey)) {
    $SigningKey = Read-DotEnvValue -Path ".env.example" -Name "JWT_SIGNING_KEY"
}

if ([string]::IsNullOrWhiteSpace($SigningKey) -or [System.Text.Encoding]::UTF8.GetByteCount($SigningKey) -lt 32) {
    throw "Signing key is required and must be at least 32 bytes. Set JWT_SIGNING_KEY in .env or pass -SigningKey."
}

$now = [DateTimeOffset]::UtcNow
$claims = [ordered]@{
    sub = $Subject
    name = "Demo User"
    iss = $Issuer
    aud = $Audience
    iat = $now.ToUnixTimeSeconds()
    nbf = $now.ToUnixTimeSeconds()
    exp = $now.AddHours($ExpiresInHours).ToUnixTimeSeconds()
    role = $Roles
}

$header = [ordered]@{
    alg = "HS256"
    typ = "JWT"
}

$encodedHeader = ConvertTo-Base64Url -Bytes (ConvertFrom-PlainJson $header)
$encodedPayload = ConvertTo-Base64Url -Bytes (ConvertFrom-PlainJson $claims)
$unsignedToken = "$encodedHeader.$encodedPayload"

$hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($SigningKey))
$signature = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($unsignedToken))
$encodedSignature = ConvertTo-Base64Url -Bytes $signature
$token = "$unsignedToken.$encodedSignature"

Write-Host ""
Write-Host "Demo JWT generated." -ForegroundColor Green
Write-Host "Issuer:   $Issuer"
Write-Host "Audience: $Audience"
Write-Host "Subject:  $Subject"
Write-Host "Roles:    $($Roles -join ', ')"
Write-Host "Expires:  $($now.AddHours($ExpiresInHours).ToLocalTime())"
Write-Host ""
Write-Host "Paste this token in the React dashboard: Configurar conexion > Token bearer"
Write-Host ""
Write-Output $token
