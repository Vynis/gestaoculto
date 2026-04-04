param(
    [string]$Version,
    [switch]$SkipZip
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
    $systemWebServer = $webConfigXml.configuration.location.'system.webServer'
    if ($null -ne $systemWebServer) {
        $aspNetCoreHandler = $systemWebServer.handlers.add | Where-Object { $_.name -eq 'aspNetCore' } | Select-Object -First 1
        if ($null -ne $aspNetCoreHandler) {
            $aspNetCoreHandler.modules = 'AspNetCoreModule'
        }

        $aspNetCoreConfig = $systemWebServer.aspNetCore
        if ($null -ne $aspNetCoreConfig -and $aspNetCoreConfig.HasAttribute('hostingModel')) {
            $aspNetCoreConfig.RemoveAttribute('hostingModel')
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
if (-not $SkipZip) {
    Write-Host "Frontend ZIP:    $(Join-Path $publishDir 'frontend.zip')"
    Write-Host "Backend ZIP:     $(Join-Path $publishDir 'backend.zip')"
}
