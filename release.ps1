param(
    [string]$Version,
    [switch]$SkipZip,
    [string]$SentryDsn,
    [string]$SentryEnvironment = 'Production',
    [bool]$DebugEnableErrorTestEndpoint = $false,
    [string]$DebugErrorTestToken,
    [string]$TelegramBotToken,
    [string]$TelegramWebhookSecret
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory
    )

    Write-Host "PS $WorkingDirectory> $Command" -ForegroundColor DarkGray
    Push-Location $WorkingDirectory
    try {
        $global:LASTEXITCODE = 0
        Invoke-Expression $Command
        if (-not $?) {
            throw "Command failed: $Command"
        }

        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code ${LASTEXITCODE}: $Command"
        }
    }
    finally {
        Pop-Location
    }
}

function Set-WebConfigAspNetCoreEnvVar {
    param(
        [Parameter(Mandatory = $true)][xml]$Xml,
        [Parameter(Mandatory = $true)]$EnvironmentVariablesNode,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $targetNode = $null
    foreach ($childNode in @($EnvironmentVariablesNode.ChildNodes)) {
        if ($childNode.NodeType -ne [System.Xml.XmlNodeType]::Element) {
            continue
        }

        if ($childNode.Name -ne 'environmentVariable' -and $childNode.Name -ne 'add') {
            continue
        }

        if ($childNode.GetAttribute('name') -eq $Name) {
            $targetNode = $childNode
            break
        }
    }

    if ($null -eq $targetNode) {
        $targetNode = $Xml.CreateElement('environmentVariable')
        $targetNode.SetAttribute('name', $Name)
        [void]$EnvironmentVariablesNode.AppendChild($targetNode)
    }

    $targetNode.SetAttribute('value', $Value)
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$frontendDir = Join-Path $root 'frontend'
$backendProject = Join-Path $root 'backend/src/GestaoCulto.API/GestaoCulto.API.csproj'
$publishDir = Join-Path $root 'publish'
$publishFrontendDir = Join-Path $publishDir 'frontend'
$publishBackendDir = Join-Path $publishDir 'backend'
$versionFile = Join-Path $root 'VERSION'

if (-not (Test-Path $versionFile)) {
    throw "VERSION file not found at: $versionFile"
}

if ($Version) {
    if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z]+)*$') {
        throw "Invalid version format: $Version"
    }

    Write-Step "Updating VERSION to $Version"
    Set-Content -Path $versionFile -Value $Version -Encoding utf8
}

$currentVersion = (Get-Content -Path $versionFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($currentVersion)) {
    throw 'VERSION file is empty.'
}

if ([string]::IsNullOrWhiteSpace($SentryDsn) -and -not [string]::IsNullOrWhiteSpace($env:SENTRY_DSN)) {
    $SentryDsn = $env:SENTRY_DSN
}

if ([string]::IsNullOrWhiteSpace($TelegramBotToken) -and -not [string]::IsNullOrWhiteSpace($env:TELEGRAM_BOT_TOKEN)) {
    $TelegramBotToken = $env:TELEGRAM_BOT_TOKEN
}

if ([string]::IsNullOrWhiteSpace($TelegramWebhookSecret) -and -not [string]::IsNullOrWhiteSpace($env:TELEGRAM_WEBHOOK_SECRET)) {
    $TelegramWebhookSecret = $env:TELEGRAM_WEBHOOK_SECRET
}

$dbConnectionString = $env:DB_CONNECTION_STRING
$smtpPassword = $env:SMTP_PASSWORD
$jwtKey = $env:JWT_KEY
$adminDefaultPassword = $env:ADMIN_DEFAULT_PASSWORD
$googleClientId = $env:GOOGLE_CLIENT_ID
$googleClientSecret = $env:GOOGLE_CLIENT_SECRET

$missingProductionSecrets = @()
if ([string]::IsNullOrWhiteSpace($dbConnectionString)) { $missingProductionSecrets += 'DB_CONNECTION_STRING' }
if ([string]::IsNullOrWhiteSpace($smtpPassword)) { $missingProductionSecrets += 'SMTP_PASSWORD' }
if ([string]::IsNullOrWhiteSpace($jwtKey)) { $missingProductionSecrets += 'JWT_KEY' }
if ([string]::IsNullOrWhiteSpace($adminDefaultPassword)) { $missingProductionSecrets += 'ADMIN_DEFAULT_PASSWORD' }
if ($missingProductionSecrets.Count -gt 0) {
    throw "Segredos obrigatorios de producao ausentes: $($missingProductionSecrets -join ', ')."
}

$googleConfigParcial = [string]::IsNullOrWhiteSpace($googleClientId) -xor [string]::IsNullOrWhiteSpace($googleClientSecret)
if ($googleConfigParcial) {
    throw 'Configure GOOGLE_CLIENT_ID e GOOGLE_CLIENT_SECRET em conjunto.'
}

$telegramConfigParcial = [string]::IsNullOrWhiteSpace($TelegramBotToken) -xor [string]::IsNullOrWhiteSpace($TelegramWebhookSecret)
if ($telegramConfigParcial) {
    throw 'Configure TELEGRAM_BOT_TOKEN e TELEGRAM_WEBHOOK_SECRET em conjunto.'
}

Write-Step "Preparing folders"
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
if (Test-Path $publishFrontendDir) { Remove-Item $publishFrontendDir -Recurse -Force }
if (Test-Path $publishBackendDir) { Remove-Item $publishBackendDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishFrontendDir -Force | Out-Null
New-Item -ItemType Directory -Path $publishBackendDir -Force | Out-Null

Write-Step 'Generating frontend version metadata'
Invoke-Step -WorkingDirectory $frontendDir -Command 'npm run prepare:version'

Write-Step 'Building frontend for KingHost'
Invoke-Step -WorkingDirectory $frontendDir -Command "C:\Progra~1\nodejs\node.exe ./node_modules/@angular/cli/bin/ng build --configuration production --output-path `"$publishFrontendDir`""

Write-Step 'Publishing backend (Release)'
Invoke-Step -WorkingDirectory $root -Command "dotnet publish `"$backendProject`" -c Release -o `"$publishBackendDir`""

Write-Step 'Normalizing backend web.config for KingHost'
$backendWebConfigPath = Join-Path $publishBackendDir 'web.config'
if (Test-Path $backendWebConfigPath) {
    [xml]$webConfigXml = Get-Content -Path $backendWebConfigPath -Raw
    $systemWebServer = $webConfigXml.SelectSingleNode('/configuration/location/system.webServer')
    if ($null -ne $systemWebServer) {
        $aspNetCoreHandler = $systemWebServer.handlers.add | Where-Object { $_.name -eq 'aspNetCore' } | Select-Object -First 1
        if ($null -ne $aspNetCoreHandler) {
            $aspNetCoreHandler.modules = 'AspNetCoreModule'
        }

        $aspNetCoreConfig = $systemWebServer.SelectSingleNode('aspNetCore')
        if ($null -ne $aspNetCoreConfig -and $aspNetCoreConfig.Attributes['hostingModel']) {
            [void]$aspNetCoreConfig.Attributes.RemoveNamedItem('hostingModel')
        }

        $shouldInjectDebugVars = $DebugEnableErrorTestEndpoint -or -not [string]::IsNullOrWhiteSpace($DebugErrorTestToken)
        $shouldInjectSentryVars = -not [string]::IsNullOrWhiteSpace($SentryDsn)
        $shouldInjectTelegramVars = -not [string]::IsNullOrWhiteSpace($TelegramBotToken)
        $shouldInjectGoogleVars = -not [string]::IsNullOrWhiteSpace($googleClientId)
        if ($null -ne $aspNetCoreConfig) {
            $environmentVariablesNode = $aspNetCoreConfig.SelectSingleNode('environmentVariables')
            if ($null -eq $environmentVariablesNode) {
                $environmentVariablesNode = $webConfigXml.CreateElement('environmentVariables')
                [void]$aspNetCoreConfig.AppendChild($environmentVariablesNode)
            }

            Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'ConnectionStrings__DefaultConnection' -Value $dbConnectionString
            Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Email__Smtp__Password' -Value $smtpPassword
            Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Jwt__Key' -Value $jwtKey
            Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'AdminPasswordReset__DefaultPassword' -Value $adminDefaultPassword

            if ($shouldInjectSentryVars) {
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Sentry__Dsn' -Value $SentryDsn
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Sentry__Environment' -Value $SentryEnvironment
            }

            if ($shouldInjectDebugVars) {
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Debug__EnableErrorTestEndpoint' -Value $DebugEnableErrorTestEndpoint.ToString().ToLowerInvariant()
            }

            if (-not [string]::IsNullOrWhiteSpace($DebugErrorTestToken)) {
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Debug__ErrorTestToken' -Value $DebugErrorTestToken
            }

            if ($shouldInjectTelegramVars) {
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Telegram__Enabled' -Value 'true'
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Telegram__BotToken' -Value $TelegramBotToken
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'Telegram__WebhookSecret' -Value $TelegramWebhookSecret
            }

            if ($shouldInjectGoogleVars) {
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'GoogleAuth__ClientId' -Value $googleClientId
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'GoogleCalendar__ClientId' -Value $googleClientId
                Set-WebConfigAspNetCoreEnvVar -Xml $webConfigXml -EnvironmentVariablesNode $environmentVariablesNode -Name 'GoogleCalendar__ClientSecret' -Value $googleClientSecret
            }
        }
    }

    $webConfigXml.Save($backendWebConfigPath)
}

Write-Step 'Creating backend version.json for runtime metadata'
$shortCommit = 'unknown'
try {
    $shortCommit = (git -C $root rev-parse --short HEAD).Trim()
}
catch {
    $shortCommit = 'unknown'
}

$buildDateUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$backendVersionPayload = [ordered]@{
    name = 'gestaoculto-api'
    version = $currentVersion
    commit = $shortCommit
    buildDate = $buildDateUtc
    environment = 'Production'
} | ConvertTo-Json

Set-Content -Path (Join-Path $publishBackendDir 'version.json') -Value $backendVersionPayload -Encoding utf8

if (-not $SkipZip) {
    Write-Step 'Creating ZIP packages'
    $frontendZip = Join-Path $publishDir 'frontend.zip'
    $backendZip = Join-Path $publishDir 'backend.zip'
    if (Test-Path $frontendZip) { Remove-Item $frontendZip -Force }
    if (Test-Path $backendZip) { Remove-Item $backendZip -Force }

    Compress-Archive -Path (Join-Path $publishFrontendDir '*') -DestinationPath $frontendZip
    Compress-Archive -Path (Join-Path $publishBackendDir '*') -DestinationPath $backendZip
}

Write-Step 'Done'
Write-Host "Version: $currentVersion"
Write-Host "Frontend publish: $publishFrontendDir"
Write-Host "Backend publish:  $publishBackendDir"
if (-not [string]::IsNullOrWhiteSpace($SentryDsn)) {
    Write-Host "Sentry web.config: enabled ($SentryEnvironment)"
}
else {
    Write-Host 'Sentry web.config: skipped (set -SentryDsn or SENTRY_DSN env var)'
}
if ($DebugEnableErrorTestEndpoint -or -not [string]::IsNullOrWhiteSpace($DebugErrorTestToken)) {
    Write-Host "Debug endpoint web.config: enabled=$($DebugEnableErrorTestEndpoint.ToString().ToLowerInvariant())"
}
else {
    Write-Host 'Debug endpoint web.config: skipped'
}
if (-not [string]::IsNullOrWhiteSpace($TelegramBotToken)) {
    Write-Host 'Telegram web.config: enabled'
}
else {
    Write-Host 'Telegram web.config: skipped (set TELEGRAM_BOT_TOKEN and TELEGRAM_WEBHOOK_SECRET)'
}
if (-not $SkipZip) {
    Write-Host "Frontend ZIP:    $(Join-Path $publishDir 'frontend.zip')"
    Write-Host "Backend ZIP:     $(Join-Path $publishDir 'backend.zip')"
}
