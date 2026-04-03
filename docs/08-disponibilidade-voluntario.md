# Gestão de Culto - Etapa 8 (Disponibilidade do Voluntário por Culto)

## Objetivo

Adicionar uma camada de **intenção de serviço** do voluntário, separada da escala oficial.

## Regras implementadas

- A disponibilidade não cria escala automática.
- Escala oficial continua com gestão/liderança.
- Voluntário só responde para cultos futuros.
- Voluntário só seleciona ministérios aos quais pertence.
- Voluntário pode atualizar a resposta até a data do culto.

## Banco de dados

- `status_disponibilidade_voluntario`
- `disponibilidade_culto_voluntario`
- `disponibilidade_culto_voluntario_ministerio`

Com índices para filtro por culto, voluntário, status e ministério.

## Backend

### Portal do voluntário

- `GET /api/portal-voluntario/cultos-planejados`
- `GET /api/portal-voluntario/culto/{cultoId}/disponibilidade`
- `PUT /api/portal-voluntario/culto/{cultoId}/disponibilidade`

### Gestão e liderança

- `GET /api/disponibilidades`
- `GET /api/disponibilidades/status`

Filtros por culto, ministério e status, com visibilidade restrita para perfil `LIDER_MINISTERIO` aos ministérios liderados.

## Frontend

### Área do voluntário

- Calendário com cultos planejados do mês.
- Status da resposta (não respondido, disponível, indisponível, escalado).
- Modal simples para informar disponibilidade, ministérios e observação.

### Área de gestão/liderança

- Nova tela: `/disponibilidades`
- Filtros por culto, ministério e status.
- Lista de respostas com observações e indicador de voluntário já escalado.

## Integração com escala

- A tela de disponibilidade já sinaliza quando o voluntário foi escalado oficialmente no culto.
- Não há acoplamento obrigatório entre disponibilidade e criação de escala.
