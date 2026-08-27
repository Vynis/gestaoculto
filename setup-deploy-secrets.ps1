param(
    [ValidateSet(
        'FTP_FRONT_PASS',
        'FTP_BACK_PASS',
        'TELEGRAM_BOT_TOKEN',
        'TELEGRAM_WEBHOOK_SECRET',
        'SENTRY_DSN'
    )]
    [string[]]$Name
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$secretDirectory = Join-Path $env:LOCALAPPDATA 'GestaoCulto\DeploySecrets'
$definitions = @(
    [pscustomobject]@{ Name = 'FTP_FRONT_PASS'; Prompt = 'Senha FTP frontend'; Required = $true },
    [pscustomobject]@{ Name = 'FTP_BACK_PASS'; Prompt = 'Senha FTP backend'; Required = $true },
    [pscustomobject]@{ Name = 'TELEGRAM_BOT_TOKEN'; Prompt = 'Token do bot Telegram'; Required = $true },
    [pscustomobject]@{ Name = 'TELEGRAM_WEBHOOK_SECRET'; Prompt = 'Segredo do webhook Telegram'; Required = $true },
    [pscustomobject]@{ Name = 'SENTRY_DSN'; Prompt = 'Sentry DSN (opcional; Enter remove o valor salvo)'; Required = $false }
)

if ($Name -and $Name.Count -gt 0) {
    $selectedNames = [System.Collections.Generic.HashSet[string]]::new(
        $Name,
        [System.StringComparer]::OrdinalIgnoreCase)
    $definitions = @($definitions | Where-Object { $selectedNames.Contains($_.Name) })
}

New-Item -ItemType Directory -Path $secretDirectory -Force | Out-Null

foreach ($definition in $definitions) {
    $secureValue = Read-Host -AsSecureString $definition.Prompt
    $plainValue = [System.Net.NetworkCredential]::new('', $secureValue).Password
    $secretPath = Join-Path $secretDirectory "$($definition.Name).txt"

    try {
        if ([string]::IsNullOrWhiteSpace($plainValue)) {
            if ($definition.Required) {
                throw "$($definition.Name) nao pode ficar vazio."
            }

            if (Test-Path -LiteralPath $secretPath) {
                Remove-Item -LiteralPath $secretPath -Force
            }
            Write-Host "$($definition.Name): valor opcional removido." -ForegroundColor Yellow
            continue
        }

        $encryptedValue = ConvertFrom-SecureString $secureValue
        [System.IO.File]::WriteAllText($secretPath, $encryptedValue, [System.Text.Encoding]::UTF8)
        Write-Host "$($definition.Name): salvo com DPAPI." -ForegroundColor Green
    }
    finally {
        $plainValue = $null
        $secureValue.Dispose()
    }
}

Write-Host "`nSegredos salvos em $secretDirectory" -ForegroundColor Cyan
Write-Host 'Eles so podem ser descriptografados por este usuario neste computador.'
