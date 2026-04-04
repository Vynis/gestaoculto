# Recuperacao de senha por e-mail

Foi implementado fluxo de recuperacao de senha com link para:

- Area de gestao (`/auth/recuperar-senha`)
- Portal do voluntario (`/voluntario/recuperar-acesso`)

## Fluxo

1. Usuario informa e-mail (gestao) ou identificador (voluntario)
2. API gera token temporario
3. API envia e-mail com link de redefinicao
4. Usuario abre o link e define nova senha

## Configuracao SMTP (KingHost)

Arquivo: `backend/src/GestaoCulto.API/appsettings.json`

- Host SMTP: `smtp.kinghost.net`
- Porta: `587`
- SSL/TLS: `true`
- Remetente: `gestao@igrejadecristobrasil.app.br`

## Configuracao do link de recuperacao

Arquivo: `backend/src/GestaoCulto.API/appsettings.json`

- `PasswordReset:FrontendBaseUrl`: URL base do frontend publicado
- `PasswordReset:ExpirationMinutes`: tempo de expiracao do link

## Observacao de seguranca

O ideal e mover credenciais SMTP para variaveis de ambiente no servidor (nao manter senha em arquivo versionado).
