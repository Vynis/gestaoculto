param(
    [string]$Version,
    [switch]$SkipZip,
    [string]$SentryDsn,
    [string]$SentryEnvironment = 'Production',
    [bool]$DebugEnableErrorTestEndpoint = $false,
    [string]$DebugErrorTestToken,
    [string]$TelegramBotToken,
    [string]$TelegramWebhookSecret,
    [ValidateSet('Overwrite', 'Mirror')]
    [string]$SyncMode = 'Overwrite',
    [string]$FrontendHost = 'ftp.igrejadecristobrasil.com.br',
    [string]$FrontendUser = 'igrejadecristobrasil',
    [string]$FrontendPass = $env:FTP_FRONT_PASS,
    [string]$FrontendRemotePath = '/www/gestaoculto',
    [string]$BackendHost = 'ftp.igrejadecristobrasil.app.br',
    [string]$BackendUser = 'igrejadecristobrasil',
    [string]$BackendPass = $env:FTP_BACK_PASS,
    [string]$BackendRemotePath = '/www/gestaoculto'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-DeployLog {
    param(
        [Parameter(Mandatory = $true)][string]$Message,
        [string]$Level = 'INFO'
    )

    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "[$timestamp] [$Level] $Message"
    Write-Host $line
    Add-Content -Path $script:LogFilePath -Value $line
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $root 'publish'
$logsDir = Join-Path $publishDir 'deploy-logs'
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

$timestampId = Get-Date -Format 'yyyyMMdd-HHmmss'
$script:LogFilePath = Join-Path $logsDir "deploy-$timestampId.txt"
New-Item -ItemType File -Path $script:LogFilePath -Force | Out-Null

if ([string]::IsNullOrWhiteSpace($SentryDsn) -and -not [string]::IsNullOrWhiteSpace($env:SENTRY_DSN)) {
    $SentryDsn = $env:SENTRY_DSN
}

if ([string]::IsNullOrWhiteSpace($TelegramBotToken) -and -not [string]::IsNullOrWhiteSpace($env:TELEGRAM_BOT_TOKEN)) {
    $TelegramBotToken = $env:TELEGRAM_BOT_TOKEN
}

if ([string]::IsNullOrWhiteSpace($TelegramWebhookSecret) -and -not [string]::IsNullOrWhiteSpace($env:TELEGRAM_WEBHOOK_SECRET)) {
    $TelegramWebhookSecret = $env:TELEGRAM_WEBHOOK_SECRET
}

if ([string]::IsNullOrWhiteSpace($FrontendPass)) {
    throw 'Senha FTP do frontend ausente. Use -FrontendPass ou variável FTP_FRONT_PASS.'
}

if ([string]::IsNullOrWhiteSpace($BackendPass)) {
    throw 'Senha FTP do backend ausente. Use -BackendPass ou variável FTP_BACK_PASS.'
}

try {
    Write-DeployLog 'Iniciando pipeline de release + upload FTP.'
    Write-DeployLog "SyncMode: $SyncMode"

    $releaseScript = Join-Path $root 'release.ps1'
    $releaseParams = @{
        SentryEnvironment = $SentryEnvironment
        DebugEnableErrorTestEndpoint = $DebugEnableErrorTestEndpoint
    }

    if ($SkipZip) {
        $releaseParams['SkipZip'] = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        $releaseParams['Version'] = $Version
    }

    if (-not [string]::IsNullOrWhiteSpace($SentryDsn)) {
        $releaseParams['SentryDsn'] = $SentryDsn
    }

    if (-not [string]::IsNullOrWhiteSpace($DebugErrorTestToken)) {
        $releaseParams['DebugErrorTestToken'] = $DebugErrorTestToken
    }

    if (-not [string]::IsNullOrWhiteSpace($TelegramBotToken)) {
        $releaseParams['TelegramBotToken'] = $TelegramBotToken
    }

    if (-not [string]::IsNullOrWhiteSpace($TelegramWebhookSecret)) {
        $releaseParams['TelegramWebhookSecret'] = $TelegramWebhookSecret
    }

    Write-DeployLog 'Executando release.ps1...'
    & $releaseScript @releaseParams
    Write-DeployLog 'Release concluido.'

    $ftpScript = Join-Path $root 'deploy-ftp.ps1'
    $ftpParams = @{
        SyncMode = $SyncMode
        FrontendHost = $FrontendHost
        FrontendUser = $FrontendUser
        FrontendPass = $FrontendPass
        FrontendRemotePath = $FrontendRemotePath
        BackendHost = $BackendHost
        BackendUser = $BackendUser
        BackendPass = $BackendPass
        BackendRemotePath = $BackendRemotePath
        LogFilePath = $script:LogFilePath
    }

    Write-DeployLog 'Executando deploy-ftp.ps1...'
    & $ftpScript @ftpParams
    Write-DeployLog 'Deploy FTP concluido com sucesso.'
    Write-DeployLog "Log salvo em: $script:LogFilePath"
}
catch {
    Write-DeployLog "Falha no deploy: $($_.Exception.Message)" 'ERROR'
    Write-DeployLog "Log salvo em: $script:LogFilePath" 'ERROR'
    throw
}
