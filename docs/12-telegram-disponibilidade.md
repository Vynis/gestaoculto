# Telegram - disponibilidade do voluntario

## Fluxo

O voluntario vinculado envia:

```text
/disponibilidade
```

O bot lista ate cinco cultos futuros ativos. Para cada culto, apresenta as opcoes
`Disponivel` e `Indisponivel`.

Ao escolher `Disponivel`, o bot permite marcar um ou mais ministerios vinculados ao
voluntario e exige confirmacao. A selecao e mantida em um rascunho persistente por 30
minutos. Ao escolher `Indisponivel`, a resposta e registrada imediatamente.

As respostas usam as mesmas tabelas da area do voluntario e aparecem na tela
administrativa de disponibilidades.

## Persistencia adicional

```text
telegram_disponibilidade_rascunho
telegram_disponibilidade_rascunho_ministerio
```

Essas tabelas guardam somente a selecao temporaria dos botoes. A resposta final continua
em:

```text
disponibilidade_culto_voluntario
disponibilidade_culto_voluntario_ministerio
```

## Regras compartilhadas

`IDisponibilidadeVoluntarioService` centraliza:

- voluntario ativo;
- culto ativo e futuro;
- ministerios pertencentes ao voluntario;
- exigencia de ao menos um ministerio quando disponivel;
- substituicao atomica da resposta e dos ministerios;
- consulta dos proximos cultos e do estado atual.

O endpoint do portal e o bot usam esse mesmo servico.

## Teste manual

1. Reiniciar a API para o `DbSeeder` criar as tabelas novas.
2. Manter o webhook configurado e a conta Telegram vinculada.
3. Garantir que o voluntario possui ministerios e que existem cultos ativos futuros.
4. Enviar `/disponibilidade` ao bot.
5. Escolher `Disponivel`, marcar ministerios e confirmar.
6. Verificar a resposta na area administrativa de disponibilidades.
7. Repetir o comando e alterar para `Indisponivel`.
8. Confirmar que os ministerios da resposta anterior foram removidos.
