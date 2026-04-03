# Gestão de Culto

Sistema para planejamento e operação de cultos com foco em usabilidade para usuários leigos.

## Estrutura

- `backend/` API ASP.NET Core 3.1
- `frontend/` Angular + Nebular
- `database/` script SQL inicial
- `docs/` documentação por etapas

## Subir projeto (resumo)

1. Banco:
   - execute `database/schema-inicial.sql`
2. Backend:
   - `dotnet run --project backend/src/GestaoCulto.API/GestaoCulto.API.csproj`
3. Frontend:
   - `cd frontend`
   - `npm install`
   - `npm start`

Documentação final de execução:

- `docs/06-instrucoes-finais.md`
