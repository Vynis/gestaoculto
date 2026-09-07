param(
    [string]$Version,
    [switch]$SkipZip,
    [string]$SentryEnvironment = 'Production',
    [bool]$DebugEnableErrorTestEndpoint = $false,
    [string]$DebugErrorTestToken,
    [ValidateSet('Overwrite', 'Mirror')]
    [string]$SyncMode = 'Overwrite'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$secretDirectory = Join-Path $env:LOCALAPPDATA 'GestaoCulto\DeploySecrets'
$requiredNames = @(
    'FTP_FRONT_PASS',
    'FTP_BACK_PASS',
    'TELEGRAM_BOT_TOKEN',
    'TELEGRAM_WEBHOOK_SECRET',
    'DB_CONNECTION_STRING',
    'SMTP_PASSWORD',
    'JWT_KEY',
    'ADMIN_DEFAULT_PASSWORD'
)
$optionalNames = @(
    'GOOGLE_CLIENT_ID',
    'GOOGLE_CLIENT_SECRET',
    'SENTRY_DSN'
)
$previousValues = @{}
$loadedNames = [System.Collections.Generic.List[string]]::new()

function Read-DpapiSecret {
    param(
        [Parameter(Mandatory = $true)][string]$SecretName,
        [Parameter(Mandatory = $true)][bool]$Required
    )

    $secretPath = Join-Path $secretDirectory "$SecretName.txt"
    if (-not (Test-Path -LiteralPath $secretPath)) {
        if ($Required) {
            throw "Segredo $SecretName nao configurado. Execute .\setup-deploy-secrets.ps1."
        }
        return $null
    }

    try {
        $encryptedValue = [System.IO.File]::ReadAllText($secretPath, [System.Text.Encoding]::UTF8).Trim()
        $secureValue = ConvertTo-SecureString $encryptedValue
        try {
            return [System.Net.NetworkCredential]::new('', $secureValue).Password
        }
        finally {
            $secureValue.Dispose()
        }
    }
    catch {
        throw "Nao foi possivel descriptografar $SecretName. Execute o setup novamente neste usuario e computador."
    }
}

try {
    foreach ($secretName in $requiredNames) {
        $previousValues[$secretName] = [System.Environment]::GetEnvironmentVariable($secretName, 'Process')
        $value = Read-DpapiSecret -SecretName $secretName -Required $true
        [System.Environment]::SetEnvironmentVariable($secretName, $value, 'Process')
        [void]$loadedNames.Add($secretName)
        $value = $null
    }

    foreach ($secretName in $optionalNames) {
        $value = Read-DpapiSecret -SecretName $secretName -Required $false
        if ($null -eq $value) {
            continue
        }

        $previousValues[$secretName] = [System.Environment]::GetEnvironmentVariable($secretName, 'Process')
        [System.Environment]::SetEnvironmentVariable($secretName, $value, 'Process')
        [void]$loadedNames.Add($secretName)
        $value = $null
    }

    $deployParams = @{
        SentryEnvironment = $SentryEnvironment
        DebugEnableErrorTestEndpoint = $DebugEnableErrorTestEndpoint
        SyncMode = $SyncMode
    }
    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        $deployParams['Version'] = $Version
    }
    if ($SkipZip) {
        $deployParams['SkipZip'] = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($DebugErrorTestToken)) {
        $deployParams['DebugErrorTestToken'] = $DebugErrorTestToken
    }

    & (Join-Path $PSScriptRoot 'deploy.ps1') @deployParams
}
finally {
    foreach ($secretName in $loadedNames) {
        [System.Environment]::SetEnvironmentVariable($secretName, $previousValues[$secretName], 'Process')
    }
}
