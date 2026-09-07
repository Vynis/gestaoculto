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
- `AdminPasswordReset:DefaultPassword`

Credenciais locais também devem usar User Secrets, pois os arquivos `appsettings` versionados
não possuem senhas:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "SUA_CONNECTION_STRING_LOCAL" --project src/GestaoCulto.API/GestaoCulto.API.csproj
dotnet user-secrets set "Jwt:Key" "SUA_CHAVE_JWT_LOCAL" --project src/GestaoCulto.API/GestaoCulto.API.csproj
dotnet user-secrets set "Email:Smtp:Password" "SUA_SENHA_SMTP" --project src/GestaoCulto.API/GestaoCulto.API.csproj
dotnet user-secrets set "AdminPasswordReset:DefaultPassword" "SUA_SENHA_PADRAO_FORTE" --project src/GestaoCulto.API/GestaoCulto.API.csproj
```

Em produção, salve `ADMIN_DEFAULT_PASSWORD` com `setup-deploy-secrets.ps1`; o script de release
a injeta como `AdminPasswordReset__DefaultPassword`. Essa senha é aplicada somente pela ação
administrativa de redefinição e deve ser comunicada ao usuário por canal seguro. No primeiro
acesso, o sistema exige a definição de uma senha pessoal.

## Telegram

O token do bot e o segredo do webhook não devem ser adicionados aos arquivos `appsettings`.
Para desenvolvimento local, use User Secrets:

```powershell
dotnet user-secrets set "Telegram:Enabled" "true" --project src/GestaoCulto.API/GestaoCulto.API.csproj
dotnet user-secrets set "Telegram:BotToken" "SEU_TOKEN_NOVO" --project src/GestaoCulto.API/GestaoCulto.API.csproj
dotnet user-secrets set "Telegram:WebhookSecret" "SEU_SEGREDO_ALEATORIO" --project src/GestaoCulto.API/GestaoCulto.API.csproj
```

Em produção, defina as variáveis antes do release ou deploy. Os scripts as injetam no
`web.config` publicado:

```powershell
$env:TELEGRAM_BOT_TOKEN = "SEU_TOKEN_NOVO"
$env:TELEGRAM_WEBHOOK_SECRET = "SEU_SEGREDO_ALEATORIO"
./release.ps1
```

Depois de publicar, autentique-se como `ADMIN` e execute uma vez:

```text
POST /api/telegram/configurar-webhook
```

O webhook público configurado é `/gestaoculto/api/telegram/webhook`. Na tela de voluntários,
edite um cadastro e use **Gerar link de ativação** para iniciar o vínculo.

Depois do vínculo, o voluntário pode consultar escalas com `/proximas` e informar sua
disponibilidade para cultos futuros com `/disponibilidade`.

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

### Segredos salvos no Windows

Para evitar digitar os segredos em cada publicação, execute uma vez:

```powershell
.\setup-deploy-secrets.ps1
```

Os valores são criptografados com DPAPI em
`%LOCALAPPDATA%\GestaoCulto\DeploySecrets`. Somente o mesmo usuário Windows no mesmo
computador consegue descriptografá-los. Depois, publique com:

```powershell
.\deploy-saved.ps1 -Version 0.2.0 -SentryEnvironment Production -SyncMode Overwrite
```

Para atualizar somente um segredo após uma rotação:

```powershell
.\setup-deploy-secrets.ps1 -Name TELEGRAM_BOT_TOKEN
```

O `SENTRY_DSN` é opcional. Pressionar Enter no campo correspondente remove o valor salvo.
O wrapper carrega os segredos apenas durante o deploy e restaura as variáveis de ambiente
anteriores ao terminar, inclusive em caso de falha.

O setup também solicita connection string de produção, senha SMTP e chave JWT. Google OAuth
é opcional, mas Client ID e Client Secret devem ser configurados ou removidos em conjunto.

Alternativamente, para usar `deploy.ps1` diretamente sem o armazenamento DPAPI, defina as
senhas por variável de ambiente:

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
