# Gestão de Culto - Etapa 6 (Instruções Finais)

## 1) Estado final da solução

Projeto entregue com base funcional completa para operação diária:

- Backend ASP.NET Core 3.1 em camadas (`Domain`, `Application`, `Infrastructure`, `API`).
- Banco MySQL/MariaDB 10.2 com modelagem, relacionamentos e seeds.
- Frontend Angular + Nebular com foco em simplicidade para usuários leigos.
- Fluxos integrados: login, dashboard, cultos, cronograma, escalas, operação ao vivo e convidados.

---

## 2) Pré-requisitos

- .NET SDK compatível com `netcoreapp3.1`
- MySQL/MariaDB 10.2.36+
- Node.js compatível com Angular 16
- npm

---

## 3) Configuração do banco

1. Crie o banco e tabelas rodando o script:

- `database/schema-inicial.sql`

2. O script já inclui dados iniciais:

- Perfis
- Ministérios principais
- Status de culto, etapa e presença

---

## 4) Executar backend

1. Ajuste conexão e chaves em:

- `backend/src/GestaoCulto.API/appsettings.Development.json`

2. Execute:

```bash
dotnet restore backend/GestaoCulto.sln
dotnet build backend/GestaoCulto.sln
dotnet run --project backend/src/GestaoCulto.API/GestaoCulto.API.csproj
```

3. API e documentação:

- Swagger: `http://localhost:5071/docs`

4. Usuário inicial (seed):

- E-mail: `admin@gestaoculto.local`
- Senha: `Admin@123`

---

## 5) Executar frontend

1. Configure ambiente em:

- `frontend/src/environments/environment.ts`

Ajuste:

- `apiUrl` (ex.: `http://localhost:5071/api`)
- `googleClientId`

2. Execute:

```bash
cd frontend
npm install
npm start
```

3. Acesso:

- `http://localhost:4200`

---

## 6) Roteiro rápido de validação (smoke test)

1. Entrar com `admin@gestaoculto.local`.
2. Criar um culto em **Cultos**.
3. Adicionar etapas em **Cronograma**.
4. Criar escala em **Escalas** e confirmar presença.
5. Registrar pessoa em **Convidados**.
6. Confirmar atualização no **Dashboard**.

---

## 7) Documentos gerados por etapa

- `docs/01-visao-geral-levantamento-modelagem.md`
- `docs/02-backend-aspnetcore31.md`
- `docs/03-frontend-angular-nebular.md`
- `docs/04-integracao-backend-frontend.md`
- `docs/05-refino-ux-ui.md`
- `docs/06-instrucoes-finais.md`
- `docs/07-area-voluntario.md`
- `docs/08-disponibilidade-voluntario.md`

---

## 8) Observações importantes

- `netcoreapp3.1` está em fim de suporte (warning esperado no build), mantido por requisito do projeto.
- Build do frontend pode mostrar warning de CommonJS (`eva-icons`), sem bloquear execução.
- Para produção, trocar credenciais/chaves padrão e configurar CORS/JWT com valores reais.

---

## 9) Próximas evoluções recomendadas

1. Implementar relatórios PDF/Excel completos.
2. Finalizar módulo de checklist com templates por equipe.
3. Completar gestão avançada de templates de culto (converter culto em template e duplicação avançada).
4. Adicionar paginação/filtros avançados em listas.
5. Incluir testes automatizados (unit/integration/e2e).
