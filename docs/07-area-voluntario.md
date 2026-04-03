# Gestão de Culto - Etapa 7 (Área do Voluntário)

## O que foi adicionado

- Área separada do voluntário com layout e rotas próprias.
- Fluxo de primeiro acesso com ativação por código.
- Fluxo de recuperação de acesso por código.
- Painel do voluntário com próximos compromissos e ministérios.
- Calendário mensal e lista de escala pessoal.
- Visualização de colegas apenas dos ministérios do voluntário.
- Tela de atualização dos próprios dados.

## Backend

### Novos endpoints

- `POST /api/voluntario-auth/solicitar-ativacao`
- `POST /api/voluntario-auth/ativar-acesso`
- `POST /api/voluntario-auth/solicitar-recuperacao`
- `POST /api/voluntario-auth/redefinir-acesso`
- `GET /api/portal-voluntario/painel`
- `GET /api/portal-voluntario/calendario?ano=YYYY&mes=MM`
- `GET /api/portal-voluntario/minha-escala`
- `GET /api/portal-voluntario/minha-escala/{id}/detalhe`
- `POST /api/portal-voluntario/minha-escala/{id}/confirmar`
- `GET /api/portal-voluntario/meus-ministerios`
- `GET /api/portal-voluntario/colegas-ministerio`
- `GET /api/portal-voluntario/meus-dados`
- `PUT /api/portal-voluntario/meus-dados`

### Segurança

- Endpoints do portal exigem perfil `VOLUNTARIO`.
- Confirmação de presença por voluntário validada para a própria escala.
- Regra de visibilidade limitada aos ministérios do voluntário.

## Banco de dados

### Novas estruturas

- `voluntario_acesso_ativacao`
- `voluntario_acesso_recuperacao`

### Ajustes

- `usuario.deve_trocar_senha`
- `usuario.origem_conta`

## Frontend

### Novas rotas

- `/voluntario/login`
- `/voluntario/primeiro-acesso`
- `/voluntario/recuperar-acesso`
- `/voluntario/painel`
- `/voluntario/calendario`
- `/voluntario/minha-escala`
- `/voluntario/meus-ministerios`
- `/voluntario/colegas`
- `/voluntario/meus-dados`

### Guardas

- `GestaoGuard`: mantém área administrativa separada.
- `VoluntarioGuard`: restringe o portal voluntário a perfil `VOLUNTARIO`.

## Observação

O fluxo de código de ativação/recuperação foi implementado de forma prática para uso imediato. Em produção, recomenda-se integrar envio automático por e-mail/SMS/WhatsApp e ocultar retorno do código em resposta da API.
