<#
.SYNOPSIS
    Configura los user-secrets de desarrollo de la API y de las pruebas de integración.

.DESCRIPTION
    Guarda en `dotnet user-secrets` (fuera del repositorio) la cadena de conexión de PostgreSQL,
    una clave JWT aleatoria y las credenciales del operador de prueba. Las contraseñas se toman de los
    parámetros, del archivo local `postgres-dev-credentials.txt` (ignorado por Git) o se piden por consola.

.EXAMPLE
    ./scripts/setup-dev-secrets.ps1
#>
param(
    [string] $PostgresPassword,
    [string] $OperatorPassword,
    [string] $OperatorUsername = 'portero'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $root 'backend/src/DomoNow.Parking.Api'
$testsProject = Join-Path $root 'backend/tests/DomoNow.Parking.Api.IntegrationTests'
$credentialsFile = Join-Path $root 'postgres-dev-credentials.txt'

function Read-CredentialLine([string] $prefix) {
    if (-not (Test-Path $credentialsFile)) { return $null }
    $line = Get-Content $credentialsFile | Where-Object { $_ -like "$prefix*" } | Select-Object -First 1
    if ($line) { return $line.Substring($prefix.Length).Trim() }
    return $null
}

function Read-Secret([string] $prompt) {
    $secure = Read-Host -Prompt $prompt -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}

function Set-Secret([string] $project, [string] $key, [string] $value) {
    dotnet user-secrets set $key $value --project $project | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "No se pudo guardar el secreto '$key'." }
}

if (-not $PostgresPassword) { $PostgresPassword = Read-CredentialLine 'Password:' }
if (-not $PostgresPassword) { $PostgresPassword = Read-Secret 'Contraseña del usuario postgres' }
if (-not $OperatorPassword) { $OperatorPassword = Read-CredentialLine 'Password operador:' }
if (-not $OperatorPassword) { $OperatorPassword = Read-Secret "Contraseña para el operador de prueba '$OperatorUsername'" }

$keyBytes = New-Object byte[] 48
$generator = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $generator.GetBytes($keyBytes) } finally { $generator.Dispose() }
$signingKey = [Convert]::ToBase64String($keyBytes)

$connection = "Host=localhost;Port=5432;Username=postgres;Password=$PostgresPassword"

Set-Secret $apiProject 'ConnectionStrings:Parking' "$connection;Database=domonow_parking"
Set-Secret $apiProject 'Jwt:SigningKey' $signingKey
Set-Secret $apiProject 'SeedOperator:Username' $OperatorUsername
Set-Secret $apiProject 'SeedOperator:Password' $OperatorPassword
Set-Secret $testsProject 'ConnectionStrings:ParkingTests' "$connection;Database=domonow_parking_tests"

Write-Host 'Secretos de desarrollo configurados:' -ForegroundColor Green
foreach ($project in @($apiProject, $testsProject)) {
    $keys = dotnet user-secrets list --project $project | ForEach-Object { ($_ -split ' = ')[0] }
    Write-Host "  $(Split-Path -Leaf $project): $($keys -join ', ')"
}
