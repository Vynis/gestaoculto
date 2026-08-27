# Telegram Bot PoC (minimo)

PoC simples para testar se o servidor aceita bot Telegram em Node via webhook.

## Comandos

- `/start` -> responde que o bot esta online
- `/ping` -> responde `pong`

## 1) Instalar

```bash
npm install
```

## 2) Configurar variaveis

Defina no ambiente (painel da hospedagem ou `.env`):

- `TELEGRAM_BOT_TOKEN`
- `PORT` (opcional)
- `WEBHOOK_PATH` (opcional, padrao `/telegram/webhook`)

## 3) Iniciar

```bash
npm start
```

## 4) Registrar webhook no Telegram

Substitua os valores e execute:

```bash
curl "https://api.telegram.org/bot<SEU_TOKEN>/setWebhook?url=https://SEU_DOMINIO<WEBHOOK_PATH>"
```

Exemplo:

```bash
curl "https://api.telegram.org/bot123456:ABCDEF/setWebhook?url=https://bot.seudominio.com/telegram/webhook"
```

## 5) Testar

No Telegram, abra o bot e envie:

- `/start`
- `/ping`

## Debug rapido

- Ver webhook atual:

```bash
curl "https://api.telegram.org/bot<SEU_TOKEN>/getWebhookInfo"
```

- Se nao responder, verifique HTTPS publico, token e logs da app.
