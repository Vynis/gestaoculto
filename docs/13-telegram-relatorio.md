# Telegram - relatorio temporario do culto

## Fluxo

1. O voluntario vinculado envia `/relatorio`.
2. O bot lista ate cinco cultos futuros ativos em que ele esta escalado.
3. Ao escolher um culto, o backend valida novamente a escala e cria um token aleatorio.
4. O bot apresenta um botao URL para `/relatorio-culto-publico?t=TOKEN`.
5. A pagina consulta `GET /api/relatorios/compartilhado?t=TOKEN`.

O link expira por padrao 12 horas depois do horario final previsto do culto. Se nao
houver horario final, sao consideradas tres horas de duracao. Remover o voluntario da
escala, inativar o voluntario ou inativar o culto tambem invalida o acesso.

## Seguranca

- O banco armazena somente SHA-256 do token, nunca o token original.
- O endpoint responde somente para token valido, nao revogado e nao expirado.
- A resposta publica nao inclui telefone, e-mail, presenca, confirmacao, observacoes
  internas, visitantes, convertidos, lideres ou acoes administrativas.
- Os endpoints legados por data e por ID exigem autenticacao.
- A resposta publica usa `Cache-Control: no-store`.
- O link pode ser encaminhado enquanto estiver valido; por isso o bot orienta que ele e
  pessoal e temporario.

## Configuracao

```json
"RelatorioCompartilhamento": {
  "FrontendBaseUrl": "https://igrejadecristobrasil.com.br/gestaoculto",
  "ExpirationHoursAfterCulto": 12
}
```

## Persistencia

O `DbSeeder` cria `relatorio_culto_compartilhamento` ao iniciar a API. O schema inicial
tambem contem a tabela.

## Teste manual

1. Gerar o frontend de desenvolvimento com `npm run build:development`.
2. Reiniciar a API para criar a tabela e carregar o build Angular.
3. Publicar a API com ngrok e configurar `Telegram:WebhookUrl` com essa URL HTTPS.
4. Manter `RelatorioCompartilhamento:FrontendBaseUrl` como `http://localhost:4200` no
   desenvolvimento. O backend detecta o endereço local e usa automaticamente a origem HTTPS
   do webhook, servindo o build Angular pelo mesmo tunel.
5. Vincular um voluntario que esteja escalado em um culto futuro ativo.
6. Enviar `/relatorio` e escolher o culto.
7. Abrir o botao e verificar cronograma, equipes e repertorio.
8. Remover o voluntario da escala e confirmar que o mesmo link retorna como indisponivel.
