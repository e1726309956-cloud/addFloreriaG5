# Diagnóstico del ChatBot de Nova Florería.
#
# Uso (Windows PowerShell):
#   powershell -ExecutionPolicy Bypass -File .\tools\probar-chatbot.ps1
#
# El script pide una cuenta cliente y, opcionalmente, la clave de Gemini.
# No guarda contraseñas, tokens ni claves; solo muestra la respuesta HTTP.
# No compartas la salida si contiene datos personales.

param(
    [string]$SupabaseUrl = "https://xwuttydqdvxmxzzslczp.supabase.co",
    [string]$PublishableKey = "sb_publishable_JFqZA4QhCIVN-Nn7EkZVIg_zdaSff3L",
    [string]$Modelo = "gemini-3.5-flash-lite"
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Show-HttpError {
    param($ErrorRecord)

    $detalle = $null

    if ($ErrorRecord.ErrorDetails -and
        $ErrorRecord.ErrorDetails.Message) {
        $detalle = $ErrorRecord.ErrorDetails.Message
    }

    $respuesta = $ErrorRecord.Exception.Response

    if ($respuesta) {
        $codigo = [int]$respuesta.StatusCode

        if (-not $detalle) {
            try {
                $lector = New-Object System.IO.StreamReader(
                    $respuesta.GetResponseStream())
                $detalle = $lector.ReadToEnd()
                $lector.Dispose()
            }
            catch {
                $detalle = $ErrorRecord.Exception.Message
            }
        }

        Write-Host "HTTP $codigo`: $detalle" -ForegroundColor Red
        return
    }

    Write-Host "ERROR: $($ErrorRecord.Exception.Message)" -ForegroundColor Red
}

function Read-SecretText {
    param([string]$Prompt)

    $seguro = Read-Host -Prompt $Prompt -AsSecureString
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro)

    try {
        [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero)
    }
}

Write-Host "== 1. Inicio de sesión en Supabase ==" -ForegroundColor Cyan
$correo = Read-Host "Correo de una cuenta cliente"
$password = Read-SecretText "Contraseña"

$authBody = @{
    email = $correo
    password = $password
} | ConvertTo-Json

try {
    $sesion = Invoke-RestMethod `
        -Uri "$SupabaseUrl/auth/v1/token?grant_type=password" `
        -Method Post `
        -Headers @{ apikey = $PublishableKey } `
        -ContentType "application/json" `
        -Body $authBody
}
catch {
    Show-HttpError $_
    exit 1
}

if (-not $sesion.access_token) {
    Write-Host "Supabase no devolvió un access_token." -ForegroundColor Red
    exit 1
}

Write-Host "Sesión obtenida para $($sesion.user.email)." -ForegroundColor Green

Write-Host ""
Write-Host "== 2. Llamada a la Edge Function chatbot ==" -ForegroundColor Cyan

$chatBody = @{
    mensaje = "Tengo 30 dólares, ¿qué arreglo me recomiendas?"
    historial = @()
} | ConvertTo-Json -Depth 5

try {
    $chat = Invoke-RestMethod `
        -Uri "$SupabaseUrl/functions/v1/chatbot" `
        -Method Post `
        -Headers @{
            apikey = $PublishableKey
            Authorization = "Bearer $($sesion.access_token)"
        } `
        -ContentType "application/json" `
        -Body $chatBody

    Write-Host "La función respondió:" -ForegroundColor Green
    $chat | ConvertTo-Json -Depth 10
}
catch {
    Show-HttpError $_
}

Write-Host ""
Write-Host "== 3. Prueba directa de Gemini (opcional) ==" -ForegroundColor Cyan
$probar = Read-Host "¿Probar la clave de Gemini directamente? (s/n)"

if ($probar -match '^[sS]') {
    $geminiKey = Read-SecretText "Pega la GEMINI_API_KEY (no se mostrará)"

    $geminiBody = @{
        contents = @(
            @{
                role = "user"
                parts = @(
                    @{ text = "Responde solo: OK" }
                )
            }
        )
        generationConfig = @{
            maxOutputTokens = 20
        }
    } | ConvertTo-Json -Depth 10

    try {
        $gemini = Invoke-RestMethod `
            -Uri "https://generativelanguage.googleapis.com/v1beta/models/${Modelo}:generateContent" `
            -Method Post `
            -Headers @{ "x-goog-api-key" = $geminiKey } `
            -ContentType "application/json" `
            -Body $geminiBody

        Write-Host "Gemini respondió correctamente:" -ForegroundColor Green
        $gemini | ConvertTo-Json -Depth 10
    }
    catch {
        Show-HttpError $_
    }
}
