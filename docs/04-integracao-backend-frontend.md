# Gestão de Culto - Etapa 4 (Integração Backend + Frontend)

## O que foi integrado

- Frontend conectado aos endpoints reais do backend para fluxo completo.
- Seleção por nome (culto, equipe e voluntário), evitando operação por ID.
- Escalas com nomes de voluntário/equipe/status vindos da API.

## Ajustes no backend

- `GET /api/cronograma/culto/{cultoId}` agora retorna também nome do ministério responsável.
- `GET /api/escalas/culto/{cultoId}` agora retorna nomes de voluntário, ministério e status de presença.

Arquivos:
- `backend/src/GestaoCulto.API/Controllers/CronogramaController.cs`
- `backend/src/GestaoCulto.API/Controllers/EscalasController.cs`

## Ajustes no frontend

- Serviços de cadastro para carregar ministérios e voluntários:
  - `frontend/src/app/core/services/cadastro.service.ts`
- Componentes conectados com seleção amigável:
  - `cronograma.component.*`
  - `escalas.component.*`
  - `convidados.component.*`

## Fluxos já funcionando

1. Login -> Dashboard.
2. Cultos: cadastro e listagem.
3. Cronograma: cadastro/listagem por culto.
4. Escalas: cadastro/listagem/confirmar presença.
5. Convidados: cadastro rápido e listagem por culto.

## Validação técnica

- Backend build: sucesso.
- Frontend build: sucesso.
- Avisos não bloqueantes:
  - EOL do `netcoreapp3.1`.
  - CommonJS de `eva-icons` no build Angular.
