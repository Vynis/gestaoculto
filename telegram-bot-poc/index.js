const express = require('express');
const https = require('https');

const app = express();
app.use(express.json());

const PORT = Number(process.env.PORT || 3000);
const BOT_TOKEN = process.env.TELEGRAM_BOT_TOKEN || '';
const WEBHOOK_PATH = process.env.WEBHOOK_PATH || '/telegram/webhook';

if (!BOT_TOKEN) {
  console.warn('TELEGRAM_BOT_TOKEN nao configurado. O bot nao respondera mensagens.');
}

function sendMessage(chatId, text) {
  return new Promise((resolve, reject) => {
    if (!BOT_TOKEN) {
      resolve(false);
      return;
    }

    const payload = JSON.stringify({ chat_id: chatId, text });
    const req = https.request(
      {
        hostname: 'api.telegram.org',
        path: `/bot${BOT_TOKEN}/sendMessage`,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(payload)
        }
      },
      (res) => {
        res.on('data', () => {});
        res.on('end', () => resolve(res.statusCode >= 200 && res.statusCode < 300));
      }
    );

    req.on('error', reject);
    req.write(payload);
    req.end();
  });
}

app.get('/', (_req, res) => {
  res.status(200).send('telegram-bot-poc online');
});

app.post(WEBHOOK_PATH, async (req, res) => {
  const message = req.body && req.body.message;
  const text = message && typeof message.text === 'string' ? message.text.trim() : '';
  const chatId = message && message.chat ? message.chat.id : null;

  if (!chatId || !text) {
    res.status(200).send('ok');
    return;
  }

  try {
    if (text === '/start') {
      await sendMessage(chatId, 'Bot online. Comandos: /start, /ping');
    } else if (text === '/ping') {
      await sendMessage(chatId, 'pong');
    } else {
      await sendMessage(chatId, 'Comando nao reconhecido. Use /start ou /ping.');
    }
  } catch (error) {
    console.error('Erro ao enviar mensagem ao Telegram:', error && error.message ? error.message : error);
  }

  res.status(200).send('ok');
});

app.listen(PORT, () => {
  console.log(`telegram-bot-poc rodando na porta ${PORT}`);
  console.log(`Webhook path: ${WEBHOOK_PATH}`);
});
