# Telegram - lembretes automaticos em VPS

## Status

Planejamento para implementacao futura. O webhook, o vinculo do voluntario e os comandos
basicos permanecem hospedados na API principal. Esta etapa nao faz parte do MVP atual.

## Objetivo

Enviar lembretes automaticos de escala pelo Telegram sem depender da execucao continua da
aplicacao ASP.NET Core no IIS da KingHost.

Exemplos iniciais:

- aviso quando uma escala for criada;
- lembrete 24 horas antes do culto;
- lembrete 3 horas antes do culto;
- aviso quando data, horario, funcao ou etapa forem alterados;
- cancelamento de notificacoes quando a escala deixar de ser valida.

## Arquitetura recomendada

```text
GestaoCulto.TelegramWorker (.NET 8 no VPS)
        |
        | POST autenticado a cada minuto
        v
GestaoCulto.API (KingHost)
        |
        +-- regras de escala e autorizacao
        +-- fila persistida no MySQL
        +-- envio pela Telegram Bot API
```

O worker nao acessa o MySQL diretamente. Ele apenas aciona um endpoint protegido da API.
Assim, regras de negocio, credenciais do banco e transacoes continuam centralizadas no
backend principal.

O webhook do Telegram continua apontando para a KingHost. O worker participa apenas do
envio de notificacoes programadas.

## Projeto futuro

Criar:

```text
backend/src/GestaoCulto.TelegramWorker
```

Responsabilidades:

- executar continuamente como `systemd`;
- chamar a API em intervalo configuravel, inicialmente 60 segundos;
- usar `HttpClientFactory`;
- aplicar timeout e retry com backoff;
- registrar falhas sem expor o token de autenticacao;
- disponibilizar health check ou log de ultima execucao.

O worker deve usar .NET 8 e comunicar-se com a API por HTTP. Enquanto o backend estiver em
.NET Core 3.1, o worker nao deve referenciar diretamente os projetos da solucao atual.

## Fila persistida

Criar a tabela `telegram_notificacao`:

```text
id
voluntario_id
escala_id                 nullable para avisos gerais
tipo                      NOVA_ESCALA, ALTERACAO, LEMBRETE_24H, LEMBRETE_3H
chave_idempotencia        unique
agendado_em
status                    PENDENTE, PROCESSANDO, ENVIADO, FALHA, CANCELADO
tentativas
proxima_tentativa_em
claim_id                  identifica a tentativa que reservou a mensagem
processando_desde
telegram_message_id
enviado_em
ultimo_erro
criado_em
atualizado_em
```

Exemplos de chave idempotente:

```text
escala:123:nova:v1
escala:123:lembrete:24h
escala:123:lembrete:3h
escala:123:alteracao:v4
```

A restricao unica em `chave_idempotencia` deve impedir que duas execucoes criem o mesmo
lembrete.

## Endpoint de processamento

Adicionar na API:

```http
POST /api/telegram/processar-lembretes
X-Telegram-Worker-Token: segredo
```

Requisitos:

- segredo diferente do token do bot e do segredo do webhook;
- comparacao do segredo em tempo constante;
- HTTPS obrigatorio em producao;
- lote limitado, por exemplo 20 notificacoes por chamada;
- reserva atomica por `claim_id`;
- retomada de itens presos em `PROCESSANDO` apos um lease configuravel;
- resposta curta com quantidades processadas, enviadas, canceladas e com falha;
- rate limiting e registro no Sentry.

## Regras antes do envio

A API deve consultar novamente o estado atual e cancelar a notificacao quando:

- o voluntario estiver inativo;
- a conexao Telegram estiver inativa;
- o culto estiver cancelado, inativo ou no passado;
- a escala tiver sido excluida, recusada ou substituida;
- o voluntario da escala tiver sido alterado;
- a notificacao tiver se tornado obsoleta por alteracao da data ou horario.

Datas operacionais devem usar o fuso `America/Sao_Paulo`. Timestamps de auditoria e fila
devem continuar armazenados em UTC.

## Retentativas

- HTTP 429: respeitar `retry_after` retornado pelo Telegram;
- HTTP 5xx ou erro de rede: retry exponencial;
- HTTP 403 ou bot bloqueado: marcar a conexao para analise e nao insistir continuamente;
- limite inicial sugerido: 5 tentativas;
- apos o limite, manter como `FALHA` e enviar o erro ao Sentry;
- nunca registrar o token do bot ou conteudo pessoal desnecessario.

## Execucao no Linux

Exemplo de unidade `systemd`:

```ini
[Unit]
Description=GestaoCulto Telegram Worker
After=network-online.target

[Service]
WorkingDirectory=/opt/gestaoculto-worker
ExecStart=/usr/bin/dotnet GestaoCulto.TelegramWorker.dll
Restart=always
RestartSec=10
Environment=DOTNET_ENVIRONMENT=Production
Environment=GestaoCultoApi__BaseUrl=https://igrejadecristobrasil.app.br/gestaoculto/
Environment=GestaoCultoApi__WorkerToken=SEGREDO

[Install]
WantedBy=multi-user.target
```

Comandos operacionais:

```bash
sudo systemctl daemon-reload
sudo systemctl enable gestaoculto-worker
sudo systemctl start gestaoculto-worker
sudo systemctl status gestaoculto-worker
journalctl -u gestaoculto-worker -f
```

## Alternativa simples

Para uma primeira validacao, um cron no VPS pode substituir o Worker Service:

```bash
* * * * * curl --fail --silent --show-error --max-time 30 \
  -X POST \
  -H "X-Telegram-Worker-Token: SEGREDO" \
  https://igrejadecristobrasil.app.br/gestaoculto/api/telegram/processar-lembretes
```

O Worker Service continua sendo preferivel para retries, logs, health check e evolucao.

## Etapas de implementacao

1. Implementar confirmacao e recusa de escala no bot usando uma regra compartilhada com o portal.
2. Criar a entidade, tabela e indices da outbox `telegram_notificacao`.
3. Enfileirar notificacoes ao criar ou alterar escalas.
4. Criar o dispatcher idempotente e o endpoint protegido.
5. Criar o projeto `GestaoCulto.TelegramWorker` em .NET 8.
6. Publicar o worker e configurar o servico `systemd`.
7. Ativar lembretes de 24 horas e 3 horas por configuracao.
8. Adicionar metricas, limpeza de historico e painel de falhas.

## Criterios de aceite

- reiniciar API ou worker nao duplica notificacoes logicas;
- duas execucoes simultaneas nao enviam o mesmo item;
- uma escala cancelada nao produz lembrete;
- falha temporaria do Telegram e reprocessada;
- voluntario desvinculado nao recebe mensagem;
- horarios sao calculados corretamente em `America/Sao_Paulo`;
- segredos existem apenas em variaveis de ambiente ou cofre de segredos;
- o worker volta automaticamente apos reinicio do VPS.

## Pendencias tecnicas relacionadas

- atualizar o backend de .NET Core 3.1 para .NET 8;
- substituir a evolucao de schema no `DbSeeder` por migrations controladas;
- rotacionar e retirar do repositorio credenciais atuais de banco, SMTP e JWT;
- definir politica de retencao das notificacoes e updates processados;
- confirmar se a KingHost permite todas as chamadas HTTPS de saida para o Telegram.
