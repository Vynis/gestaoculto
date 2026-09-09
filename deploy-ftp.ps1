param(
    [ValidateSet('Overwrite', 'Mirror')]
    [string]$SyncMode = 'Overwrite',
    [string]$FrontendLocalPath = (Join-Path $PSScriptRoot 'publish/frontend'),
    [string]$BackendLocalPath = (Join-Path $PSScriptRoot 'publish/backend'),
    [string]$FrontendHost = $env:FTP_FRONT_HOST,
    [string]$FrontendUser = $env:FTP_FRONT_USER,
    [string]$FrontendPass = $env:FTP_FRONT_PASS,
    [string]$FrontendRemotePath = $env:FTP_FRONT_PATH,
    [string]$BackendHost = $env:FTP_BACK_HOST,
    [string]$BackendUser = $env:FTP_BACK_USER,
    [string]$BackendPass = $env:FTP_BACK_PASS,
    [string]$BackendRemotePath = $env:FTP_BACK_PATH,
    [string]$LogFilePath
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
    if (-not [string]::IsNullOrWhiteSpace($LogFilePath)) {
        Add-Content -Path $LogFilePath -Value $line
    }
}

function New-FtpUri {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    $normalizedPath = if ($null -eq $RemotePath) { [string]::Empty } else { $RemotePath }
    $normalizedPath = $normalizedPath.Trim()
    $normalizedPath = $normalizedPath -replace '\\', '/'
    $normalizedPath = $normalizedPath.Trim('/')
    if ([string]::IsNullOrWhiteSpace($normalizedPath)) {
        return "ftp://$FtpHost/"
    }

    $segments = $normalizedPath.Split('/', [System.StringSplitOptions]::RemoveEmptyEntries)
    $encodedSegments = @()
    foreach ($segment in $segments) {
        $encodedSegments += [System.Uri]::EscapeDataString($segment)
    }

    $encodedPath = [string]::Join('/', $encodedSegments)
    return "ftp://$FtpHost/$encodedPath"
}

function Join-RemotePath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $left = ($BasePath -replace '\\', '/').TrimEnd('/')
    $right = ($RelativePath -replace '\\', '/').Trim('/').Trim()
    if ([string]::IsNullOrWhiteSpace($right)) {
        return $left
    }

    if ([string]::IsNullOrWhiteSpace($left)) {
        return "/$right"
    }

    return "$left/$right"
}

function New-FtpRequest {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass
    )

    $request = [System.Net.FtpWebRequest]::Create($Uri)
    $request.Method = $Method
    $request.Credentials = New-Object System.Net.NetworkCredential($User, $Pass)
    $request.UseBinary = $true
    $request.UsePassive = $true
    $request.KeepAlive = $false
    $request.Timeout = 60000
    $request.ReadWriteTimeout = 60000
    return $request
}

function Test-FtpPathExists {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass
    )

    try {
        $request = New-FtpRequest -Uri $Uri -Method ([System.Net.WebRequestMethods+Ftp]::ListDirectory) -User $User -Pass $Pass
        $response = $request.GetResponse()
        $response.Close()
        return $true
    }
    catch [System.Net.WebException] {
        if ($_.Exception.Response -is [System.Net.FtpWebResponse]) {
            $resp = [System.Net.FtpWebResponse]$_.Exception.Response
            $status = [int]$resp.StatusCode
            $description = $resp.StatusDescription
            $resp.Close()
            if ($status -eq 550) {
                return $false
            }
            if ($status -eq 450) {
                Write-DeployLog "Caminho FTP temporariamente indisponivel durante verificacao: $Uri | Status: $status | Detalhe: $description" 'WARN'
                Start-Sleep -Seconds 2
                return $false
            }
        }
        throw
    }
}

function Ensure-RemoteDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemoteRoot,
        [Parameter(Mandatory = $true)][string]$RelativeDirectory
    )

    $segments = ($RelativeDirectory -replace '\\', '/').Split('/', [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($segments.Count -eq 0) {
        return
    }

    $currentPath = ($RemoteRoot -replace '\\', '/').TrimEnd('/')
    foreach ($segment in $segments) {
        $currentPath = Join-RemotePath -BasePath $currentPath -RelativePath $segment
        $uri = New-FtpUri -FtpHost $FtpHost -RemotePath $currentPath

        if (Test-FtpPathExists -Uri $uri -User $User -Pass $Pass) {
            continue
        }

        try {
            $request = New-FtpRequest -Uri $uri -Method ([System.Net.WebRequestMethods+Ftp]::MakeDirectory) -User $User -Pass $Pass
            $response = [System.Net.FtpWebResponse]$request.GetResponse()
            $response.Close()
            Write-DeployLog "Pasta remota criada: $currentPath"
        }
        catch [System.Net.WebException] {
            if ($_.Exception.Response -is [System.Net.FtpWebResponse]) {
                $resp = [System.Net.FtpWebResponse]$_.Exception.Response
                $status = [int]$resp.StatusCode
                $description = $resp.StatusDescription
                $resp.Close()
                if ($status -eq 550) {
                    continue
                }
                if ($status -eq 450) {
                    Start-Sleep -Seconds 2
                    if (Test-FtpPathExists -Uri $uri -User $User -Pass $Pass) {
                        continue
                    }

                    throw "Falha ao criar pasta remota '$currentPath'. Status: $status | Detalhe: $description"
                }
            }
            throw
        }
    }
}

function Convert-FtpListLineToName {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Line
    )

    $raw = $Line.Trim()
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return [string]::Empty
    }

    $name = $raw

    if ($raw -match '^(?<perm>[\-ld])[rwx\-]{9}\s+\d+\s+\S+\s+\S+\s+\d+\s+\w+\s+\d+\s+(?:\d{2}:\d{2}|\d{4})\s+(?<name>.+)$') {
        $name = $Matches['name']
    }
    elseif ($raw -match '^\d+\s+\S+\s+\S+\s+\d+\s+\w+\s+\d+\s+(?:\d{2}:\d{2}|\d{4})\s+(?<name>.+)$') {
        $name = $Matches['name']
    }
    elseif ($raw -match '^\d{2}-\d{2}-\d{2}\s+\d{2}:\d{2}(?:AM|PM)\s+(?:<DIR>|\d+)\s+(?<name>.+)$') {
        $name = $Matches['name']
    }

    return $name.Trim()
}

function Normalize-FtpEntryName {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Name
    )

    $candidate = $Name.Trim()
    if ([string]::IsNullOrWhiteSpace($candidate)) {
        return [string]::Empty
    }

    $candidate = $candidate -replace '\\', '/'

    if ($candidate.Contains('/')) {
        $parts = $candidate.Split('/', [System.StringSplitOptions]::RemoveEmptyEntries)
        if ($parts.Length -gt 0) {
            $candidate = $parts[$parts.Length - 1]
        }
    }

    $candidate = $candidate.Trim()
    if ([string]::IsNullOrWhiteSpace($candidate)) {
        return [string]::Empty
    }

    if ($candidate -eq '.' -or $candidate -eq '..') {
        return [string]::Empty
    }

    return $candidate
}

function Get-FtpDirectoryEntries {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    $uri = New-FtpUri -FtpHost $FtpHost -RemotePath $RemotePath
    $maxAttempts = 3
    $namesSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $response = $null
        $stream = $null
        $reader = $null

        try {
            $request = New-FtpRequest -Uri $uri -Method ([System.Net.WebRequestMethods+Ftp]::ListDirectory) -User $User -Pass $Pass
            $response = $request.GetResponse()
            $stream = $response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $content = $reader.ReadToEnd()

            $lines = $content -split "`r?`n"
            foreach ($lineRaw in $lines) {
                $line = if ($null -eq $lineRaw) { [string]::Empty } else { $lineRaw }
                if ([string]::IsNullOrWhiteSpace($line)) {
                    continue
                }

                $name = Convert-FtpListLineToName -Line $line
                $name = Normalize-FtpEntryName -Name $name
                if ([string]::IsNullOrWhiteSpace($name)) {
                    continue
                }

                [void]$namesSet.Add($name)
            }

            break
        }
        catch {
            if ($attempt -lt $maxAttempts) {
                Write-DeployLog "Falha transitoria ao listar diretorio remoto '$RemotePath' (tentativa $attempt/$maxAttempts): $($_.Exception.Message)" 'WARN'
                Start-Sleep -Milliseconds 400
                continue
            }

            throw
        }
        finally {
            if ($null -ne $reader) { $reader.Close() }
            if ($null -ne $stream) { $stream.Close() }
            if ($null -ne $response) { $response.Close() }
        }
    }

    $entries = @()
    foreach ($name in $namesSet) {
        $childPath = Join-RemotePath -BasePath $RemotePath -RelativePath $name
        $isDirectory = Test-RemoteDirectory -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $childPath
        $entries += [PSCustomObject]@{
            Name = $name
            IsDirectory = $isDirectory
        }
    }

    return $entries
}

function Test-RemoteDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    if (Test-RemoteFile -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $RemotePath) {
        return $false
    }

    try {
        $uri = New-FtpUri -FtpHost $FtpHost -RemotePath $RemotePath
        $request = New-FtpRequest -Uri $uri -Method ([System.Net.WebRequestMethods+Ftp]::ListDirectory) -User $User -Pass $Pass
        $response = $request.GetResponse()
        $response.Close()
        return $true
    }
    catch [System.Net.WebException] {
        if ($_.Exception.Response -is [System.Net.FtpWebResponse]) {
            $resp = [System.Net.FtpWebResponse]$_.Exception.Response
            $resp.Close()
        }

        return $false
    }
}

function Test-RemoteFile {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    try {
        $uri = New-FtpUri -FtpHost $FtpHost -RemotePath $RemotePath
        $request = New-FtpRequest -Uri $uri -Method ([System.Net.WebRequestMethods+Ftp]::GetFileSize) -User $User -Pass $Pass
        $response = $request.GetResponse()
        $response.Close()
        return $true
    }
    catch [System.Net.WebException] {
        if ($_.Exception.Response -is [System.Net.FtpWebResponse]) {
            $resp = [System.Net.FtpWebResponse]$_.Exception.Response
            $status = [int]$resp.StatusCode
            $resp.Close()

            if ($status -eq 550) {
                return $false
            }
        }

        return $false
    }
}

function Get-RemoteTree {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemoteRoot,
        [string]$RelativePath = ''
    )

    $fullPath = if ([string]::IsNullOrWhiteSpace($RelativePath)) { $RemoteRoot } else { Join-RemotePath -BasePath $RemoteRoot -RelativePath $RelativePath }
    $entries = Get-FtpDirectoryEntries -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $fullPath
    $files = @()
    $directories = @()
    $currentLeaf = [System.IO.Path]::GetFileName(($RelativePath -replace '/', '\\'))

    foreach ($entry in $entries) {
        $entryRelativePath = if ([string]::IsNullOrWhiteSpace($RelativePath)) { $entry.Name } else { "$RelativePath/$($entry.Name)" }
        if ($entry.IsDirectory) {
            $directories += $entryRelativePath

            if (-not [string]::IsNullOrWhiteSpace($currentLeaf) -and [string]::Equals($entry.Name, $currentLeaf, [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $subTree = Get-RemoteTree -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $RemoteRoot -RelativePath $entryRelativePath
            $files += $subTree.Files
            $directories += $subTree.Directories
        }
        else {
            $files += $entryRelativePath
        }
    }

    return [PSCustomObject]@{
        Files = $files
        Directories = $directories
    }
}

function Upload-File {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$LocalFile,
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    $bytes = [System.IO.File]::ReadAllBytes($LocalFile)
    $maxAttempts = 4

    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $request = $null
        $requestStream = $null
        $response = $null

        try {
            $uri = New-FtpUri -FtpHost $FtpHost -RemotePath $RemotePath
            $request = New-FtpRequest -Uri $uri -Method ([System.Net.WebRequestMethods+Ftp]::UploadFile) -User $User -Pass $Pass
            $request.ContentLength = $bytes.Length
            $requestStream = $request.GetRequestStream()
            $requestStream.Write($bytes, 0, $bytes.Length)
            $requestStream.Close()
            $requestStream = $null

            $response = [System.Net.FtpWebResponse]$request.GetResponse()
            $response.Close()
            return
        }
        catch [System.Net.WebException] {
            $status = $null
            $description = $_.Exception.Message
            if ($_.Exception.Response -is [System.Net.FtpWebResponse]) {
                $resp = [System.Net.FtpWebResponse]$_.Exception.Response
                $status = [int]$resp.StatusCode
                $description = $resp.StatusDescription
                $resp.Close()
            }

            if ($status -eq 450 -and $attempt -lt $maxAttempts) {
                $waitSeconds = $attempt * 2
                Write-DeployLog "Arquivo remoto ocupado durante upload '$RemotePath' (tentativa $attempt/$maxAttempts). Nova tentativa em ${waitSeconds}s." 'WARN'
                Start-Sleep -Seconds $waitSeconds
                continue
            }

            if ($null -ne $status) {
                throw "Falha no upload FTP. Path remoto: $RemotePath | Status: $status | Detalhe: $description"
            }

            throw "Falha no upload FTP. Path remoto: $RemotePath | Erro: $description"
        }
        finally {
            if ($null -ne $requestStream) { $requestStream.Close() }
            if ($null -ne $response) { $response.Close() }
        }
    }
}

function Test-RemoteWriteAccess {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemoteRoot
    )

    $tempLocalFile = [System.IO.Path]::GetTempFileName()
    # Alguns servidores FTP recusam arquivos ocultos mesmo quando a pasta permite escrita.
    $tempRemoteName = "deploy-write-test-$([Guid]::NewGuid().ToString('N')).tmp"
    $tempRemotePath = Join-RemotePath -BasePath $RemoteRoot -RelativePath $tempRemoteName

    try {
        [System.IO.File]::WriteAllText($tempLocalFile, 'write-test')
        Upload-File -FtpHost $FtpHost -User $User -Pass $Pass -LocalFile $tempLocalFile -RemotePath $tempRemotePath
        Remove-RemoteFile -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $tempRemotePath
        return $true
    }
    catch {
        Write-DeployLog "Sem permissao de escrita no caminho remoto '$RemoteRoot' em $FtpHost. Detalhe: $($_.Exception.Message)" 'WARN'
        return $false
    }
    finally {
        if (Test-Path $tempLocalFile) {
            Remove-Item -Path $tempLocalFile -Force
        }
    }
}

function Set-AppOfflineMode {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemoteRoot,
        [Parameter(Mandatory = $true)][bool]$Enabled
    )

    $offlineRemotePath = Join-RemotePath -BasePath $RemoteRoot -RelativePath 'app_offline.htm'

    if ($Enabled) {
        $tempLocalFile = [System.IO.Path]::GetTempFileName()
        try {
            $content = @(
                '<!doctype html>',
                '<html><head><meta charset="utf-8"><title>Atualizacao</title></head>',
                '<body><h1>Sistema em atualizacao</h1><p>Tente novamente em instantes.</p></body></html>'
            ) -join "`r`n"
            [System.IO.File]::WriteAllText($tempLocalFile, $content)
            Upload-File -FtpHost $FtpHost -User $User -Pass $Pass -LocalFile $tempLocalFile -RemotePath $offlineRemotePath
            Start-Sleep -Seconds 2
            Write-DeployLog "app_offline.htm ativado em $RemoteRoot"
        }
        finally {
            if (Test-Path $tempLocalFile) {
                Remove-Item -Path $tempLocalFile -Force
            }
        }
    }
    else {
        try {
            Remove-RemoteFile -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $offlineRemotePath
            Write-DeployLog "app_offline.htm removido de $RemoteRoot"
        }
        catch {
            Write-DeployLog "Nao foi possivel remover app_offline.htm de ${RemoteRoot}: $($_.Exception.Message)" 'WARN'
        }
    }
}

function Remove-RemoteFile {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    $uri = New-FtpUri -FtpHost $FtpHost -RemotePath $RemotePath
    try {
        $request = New-FtpRequest -Uri $uri -Method ([System.Net.WebRequestMethods+Ftp]::DeleteFile) -User $User -Pass $Pass
        $response = [System.Net.FtpWebResponse]$request.GetResponse()
        $response.Close()
    }
    catch [System.Net.WebException] {
        if ($_.Exception.Response -is [System.Net.FtpWebResponse]) {
            $resp = [System.Net.FtpWebResponse]$_.Exception.Response
            $status = [int]$resp.StatusCode
            $detail = $resp.StatusDescription
            $resp.Close()
            throw "Falha ao remover arquivo remoto '$RemotePath'. Status: $status | Detalhe: $detail"
        }

        throw "Falha ao remover arquivo remoto '$RemotePath'. Erro: $($_.Exception.Message)"
    }
}

function Remove-RemoteDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    $uri = New-FtpUri -FtpHost $FtpHost -RemotePath $RemotePath
    try {
        $request = New-FtpRequest -Uri $uri -Method ([System.Net.WebRequestMethods+Ftp]::RemoveDirectory) -User $User -Pass $Pass
        $response = [System.Net.FtpWebResponse]$request.GetResponse()
        $response.Close()
    }
    catch [System.Net.WebException] {
        if ($_.Exception.Response -is [System.Net.FtpWebResponse]) {
            $resp = [System.Net.FtpWebResponse]$_.Exception.Response
            $status = [int]$resp.StatusCode
            $detail = $resp.StatusDescription
            $resp.Close()
            throw "Falha ao remover pasta remota '$RemotePath'. Status: $status | Detalhe: $detail"
        }

        throw "Falha ao remover pasta remota '$RemotePath'. Erro: $($_.Exception.Message)"
    }
}

function Remove-RemoteEntryBestEffort {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemotePath
    )

    $fileError = $null
    try {
        Remove-RemoteFile -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $RemotePath
        return 'file'
    }
    catch {
        $fileError = $_.Exception.Message
    }

    $dirError = $null
    try {
        Remove-RemoteDirectory -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $RemotePath
        return 'directory'
    }
    catch {
        $dirError = $_.Exception.Message
    }

    throw "Falha ao remover item remoto '$RemotePath'. Arquivo: $fileError | Pasta: $dirError"
}

function Get-LocalRelativePaths {
    param(
        [Parameter(Mandatory = $true)][string]$LocalRoot
    )

    $fullRoot = (Resolve-Path $LocalRoot).Path
    $files = Get-ChildItem -Path $fullRoot -Recurse -File | ForEach-Object {
        $_.FullName.Substring($fullRoot.Length).TrimStart('\').Replace('\', '/')
    }

    $directories = Get-ChildItem -Path $fullRoot -Recurse -Directory | ForEach-Object {
        $_.FullName.Substring($fullRoot.Length).TrimStart('\').Replace('\', '/')
    }

    return [PSCustomObject]@{
        Root = $fullRoot
        Files = $files
        Directories = $directories
    }
}

function Get-RemoteRootCandidates {
    param(
        [Parameter(Mandatory = $true)][string]$RemoteRoot
    )

    $normalized = ($RemoteRoot -replace '\\', '/').Trim()
    $normalized = $normalized.TrimEnd('/')
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return @('/')
    }

    $candidates = New-Object System.Collections.Generic.List[string]

    $addCandidate = {
        param([string]$value)
        if ([string]::IsNullOrWhiteSpace($value)) {
            return
        }

        if (-not $candidates.Contains($value)) {
            [void]$candidates.Add($value)
        }
    }

    & $addCandidate $normalized

    $withoutLeadingSlash = $normalized.TrimStart('/')
    if (-not [string]::IsNullOrWhiteSpace($withoutLeadingSlash)) {
        & $addCandidate $withoutLeadingSlash
    }

    if ($normalized -match '^/www/(?<tail>.+)$') {
        $tail = $Matches['tail']
        & $addCandidate "/$tail"
        & $addCandidate $tail
    }

    if ($normalized -match '^www/(?<tail>.+)$') {
        $tail = $Matches['tail']
        & $addCandidate "/$tail"
    }

    return $candidates.ToArray()
}

function Resolve-RemoteRoot {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemoteRoot
    )

    $candidates = Get-RemoteRootCandidates -RemoteRoot $RemoteRoot
    foreach ($candidate in $candidates) {
        $uri = New-FtpUri -FtpHost $FtpHost -RemotePath $candidate
        $exists = Test-FtpPathExists -Uri $uri -User $User -Pass $Pass
        if (-not $exists) {
            continue
        }

        $canWrite = Test-RemoteWriteAccess -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $candidate
        if ($canWrite) {
            return $candidate
        }
    }

    return $null
}

function Ensure-RemoteRootExists {
    param(
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$RemoteRoot
    )

    $relative = ($RemoteRoot -replace '\\', '/').Trim().Trim('/')
    if ([string]::IsNullOrWhiteSpace($relative)) {
        return
    }

    Ensure-RemoteDirectory -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot '' -RelativeDirectory $relative
}

function Sync-FtpTarget {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$FtpHost,
        [Parameter(Mandatory = $true)][string]$User,
        [Parameter(Mandatory = $true)][string]$Pass,
        [Parameter(Mandatory = $true)][string]$LocalRoot,
        [Parameter(Mandatory = $true)][string]$RemoteRoot,
        [Parameter(Mandatory = $true)][string]$Mode,
        [bool]$UseAppOffline = $false
    )

    if (-not (Test-Path $LocalRoot)) {
        throw "Pasta local nao encontrada para ${Label}: $LocalRoot"
    }

    if ([string]::IsNullOrWhiteSpace($FtpHost) -or [string]::IsNullOrWhiteSpace($User) -or [string]::IsNullOrWhiteSpace($Pass) -or [string]::IsNullOrWhiteSpace($RemoteRoot)) {
        throw "Configuracao FTP incompleta para $Label. Verifique host, usuario, senha e caminho remoto."
    }

    Write-DeployLog "Iniciando sincronizacao $Label ($Mode) -> ${FtpHost}:$RemoteRoot"

    $resolvedRemoteRoot = Resolve-RemoteRoot -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $RemoteRoot
    if ($null -eq $resolvedRemoteRoot) {
        Write-DeployLog "Caminho remoto nao encontrado para ${Label}. Tentando criar: $RemoteRoot" 'WARN'
        Ensure-RemoteRootExists -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $RemoteRoot
        $resolvedRemoteRoot = Resolve-RemoteRoot -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $RemoteRoot
    }

    if ($null -eq $resolvedRemoteRoot) {
        $tried = (Get-RemoteRootCandidates -RemoteRoot $RemoteRoot) -join ', '
        throw "Caminho remoto nao encontrado para ${Label}: $RemoteRoot. Tentativas: $tried"
    }

    if (-not [string]::Equals($resolvedRemoteRoot, $RemoteRoot, [System.StringComparison]::Ordinal)) {
        Write-DeployLog "Usando caminho remoto alternativo para ${Label}: $resolvedRemoteRoot" 'WARN'
    }

    $localTree = Get-LocalRelativePaths -LocalRoot $LocalRoot

    $uploadCount = 0
    $deleteFileCount = 0
    $deleteDirCount = 0

    try {
        if ($UseAppOffline) {
            Set-AppOfflineMode -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $resolvedRemoteRoot -Enabled $true
        }

        foreach ($relativeDir in $localTree.Directories) {
            Ensure-RemoteDirectory -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $resolvedRemoteRoot -RelativeDirectory $relativeDir
        }

        foreach ($relativeFile in $localTree.Files) {
            $localFilePath = Join-Path $localTree.Root ($relativeFile -replace '/', '\\')
            $directoryPart = [System.IO.Path]::GetDirectoryName($relativeFile)
            if (-not [string]::IsNullOrWhiteSpace($directoryPart)) {
                Ensure-RemoteDirectory -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $resolvedRemoteRoot -RelativeDirectory $directoryPart
            }

            $remoteFilePath = Join-RemotePath -BasePath $resolvedRemoteRoot -RelativePath $relativeFile
            Upload-File -FtpHost $FtpHost -User $User -Pass $Pass -LocalFile $localFilePath -RemotePath $remoteFilePath
            $uploadCount++
        }

        if ($Mode -eq 'Mirror') {
            Write-DeployLog "Modo Mirror ativado para ${Label}: removendo itens remotos ausentes localmente."
            $remoteTree = Get-RemoteTree -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $resolvedRemoteRoot

            $localFilesSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
            foreach ($item in $localTree.Files) { [void]$localFilesSet.Add($item) }

            $localDirsSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
            foreach ($item in $localTree.Directories) { [void]$localDirsSet.Add($item) }

            $removedEntries = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

            foreach ($remoteFile in $remoteTree.Files) {
                if (-not $localFilesSet.Contains($remoteFile)) {
                    $remoteFilePath = Join-RemotePath -BasePath $resolvedRemoteRoot -RelativePath $remoteFile
                    try {
                        $removedType = Remove-RemoteEntryBestEffort -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $remoteFilePath
                        [void]$removedEntries.Add($remoteFilePath)
                        if ($removedType -eq 'directory') {
                            $deleteDirCount++
                        }
                        else {
                            $deleteFileCount++
                        }
                    }
                    catch {
                        Write-DeployLog "Nao foi possivel remover arquivo no Mirror: $remoteFilePath. Detalhe: $($_.Exception.Message)" 'WARN'
                    }
                }
            }

            $dirsToDelete = $remoteTree.Directories |
                Where-Object { -not $localDirsSet.Contains($_) } |
                Sort-Object { $_.Split('/').Count } -Descending

            foreach ($remoteDir in $dirsToDelete) {
                $remoteDirPath = Join-RemotePath -BasePath $resolvedRemoteRoot -RelativePath $remoteDir
                if ($removedEntries.Contains($remoteDirPath)) {
                    continue
                }

                try {
                    $removedType = Remove-RemoteEntryBestEffort -FtpHost $FtpHost -User $User -Pass $Pass -RemotePath $remoteDirPath
                    [void]$removedEntries.Add($remoteDirPath)
                    if ($removedType -eq 'directory') {
                        $deleteDirCount++
                    }
                    else {
                        $deleteFileCount++
                    }
                }
                catch {
                    Write-DeployLog "Nao foi possivel remover pasta no Mirror: $remoteDirPath. Detalhe: $($_.Exception.Message)" 'WARN'
                }
            }
        }
    }
    finally {
        if ($UseAppOffline) {
            Set-AppOfflineMode -FtpHost $FtpHost -User $User -Pass $Pass -RemoteRoot $resolvedRemoteRoot -Enabled $false
        }
    }

    Write-DeployLog "$Label concluido. Enviados: $uploadCount | Removidos arquivos: $deleteFileCount | Removidos pastas: $deleteDirCount"
}

if (-not [string]::IsNullOrWhiteSpace($LogFilePath)) {
    $logDir = Split-Path -Parent $LogFilePath
    if (-not [string]::IsNullOrWhiteSpace($logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }

    New-Item -ItemType File -Path $LogFilePath -Force | Out-Null
}

Write-DeployLog "Sync mode selecionado: $SyncMode"
Sync-FtpTarget -Label 'Frontend' -FtpHost $FrontendHost -User $FrontendUser -Pass $FrontendPass -LocalRoot $FrontendLocalPath -RemoteRoot $FrontendRemotePath -Mode $SyncMode
Sync-FtpTarget -Label 'Backend' -FtpHost $BackendHost -User $BackendUser -Pass $BackendPass -LocalRoot $BackendLocalPath -RemoteRoot $BackendRemotePath -Mode $SyncMode -UseAppOffline $true
Write-DeployLog 'Upload FTP finalizado com sucesso.'
