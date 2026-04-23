# Backend - Gestão de Culto

API em ASP.NET Core 3.1 com arquitetura em camadas.

## Pré-requisitos

- .NET SDK com suporte a `netcoreapp3.1`
- MySQL/MariaDB 10.2+

## Execução

```bash
dotnet restore GestaoCulto.sln
dotnet build GestaoCulto.sln
dotnet run --project src/GestaoCulto.API/GestaoCulto.API.csproj
```

Swagger:

- `http://localhost:5000/docs`

## Configuração

Arquivo: `src/GestaoCulto.API/appsettings.Development.json`

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `GoogleAuth:ClientId`

## Usuário inicial

- E-mail: `admin@gestaoculto.local`
- Senha: `Admin@123`

## Publicação com Sentry no web.config

No deploy para hospedagem compartilhada (KingHost), o script `release.ps1` consegue injetar
`Sentry__Dsn` e `Sentry__Environment` no `publish/backend/web.config`.

```powershell
# opção 1: informar no comando
.\release.ps1 -SentryDsn "https://SEU_DSN" -SentryEnvironment "Production"

# opção 2: usar variável de ambiente
$env:SENTRY_DSN = "https://SEU_DSN"
.\release.ps1 -SentryEnvironment "Production"
```

### Endpoint temporario de teste (producao)

Para habilitar o endpoint `GET /api/debug/erro-sentry-temp` durante uma janela de teste,
publique com token temporario:

```powershell
.\release.ps1 -SentryEnvironment "Production" -DebugEnableErrorTestEndpoint $true -DebugErrorTestToken "SEU_TOKEN_FORTE"
```

Chame o endpoint com header `X-Debug-Token: SEU_TOKEN_FORTE`.
Depois de validar no Sentry, desative no proximo deploy:

```powershell
.\release.ps1 -SentryEnvironment "Production" -DebugEnableErrorTestEndpoint $false
```

## Deploy automatizado via FTP

Na raiz do repositório, use `deploy.ps1` para executar release e upload FTP em sequência.

Pré-requisito: definir as senhas FTP por variável de ambiente.

```powershell
$env:FTP_FRONT_PASS = "SENHA_FRONT"
$env:FTP_BACK_PASS = "SENHA_BACK"
$env:SENTRY_DSN = "https://SEU_DSN"

.\deploy.ps1 -SentryEnvironment "Production" -SyncMode Overwrite
```

Modos de sincronização:

- `Overwrite` (padrão): envia/atualiza arquivos sem apagar remotos extras.
- `Mirror`: remove arquivos/pastas remotos que não existem localmente.

O processo gera log em `publish/deploy-logs/deploy-YYYYMMDD-HHMMSS.txt`.
