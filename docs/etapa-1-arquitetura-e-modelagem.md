# Gestão de Culto - Etapa 1 (Arquitetura e Modelagem)

## 1) Levantamento funcional consolidado

### Objetivo do produto
Plataforma web para planejar, escalar, operar e acompanhar cultos, com foco em simplicidade para usuários leigos.

### Perfis e permissões (RBAC)
- **Admin**: gestão completa de usuários, cadastros gerais e visão total.
- **Gestão de Culto**: cria cultos/templates, cronograma e escalas.
- **Líder de Ministério**: acompanha equipe, confirma presença e tarefas do setor.
- **Voluntário**: consulta escala, horário/função, confirma presença e conclui tarefas permitidas.
- **Recepção/Dados**: registra convidados/novos convertidos e pendências de atendimento.

### Módulos funcionais
1. **Dashboard**: culto mais próximo, escalas, pendências, convidados, convertidos, alertas e status geral.
2. **Cultos**: criar, editar, duplicar, gerar por template, definir status e metadados.
3. **Templates de culto**: etapas padrão, equipes padrão, checklists padrão e reaproveitamento.
4. **Cronograma**: etapas ordenadas, horário/duração, cálculo automático de término, timeline/lista, impressão.
5. **Equipes/Ministérios**: cadastro, líderes, associação de voluntários.
6. **Voluntários**: dados completos, vínculo com equipes, indisponibilidade e status ativo.
7. **Escalas**: vínculo por culto/etapa, múltiplos voluntários por tarefa, presença e observações.
8. **Convidados e novos convertidos**: fluxo rápido de registro e acompanhamento.
9. **Checklist operacional**: pré/durante/pós por equipe.
10. **Relatórios**: cultos, escalas, presença/faltas, convidados, convertidos, por equipe; exportação PDF/Excel.

### Regras de negócio principais
- Um culto possui várias etapas no cronograma.
- Uma etapa pode ter múltiplos responsáveis.
- Uma etapa pode ter equipe principal associada.
- Um voluntário participa de várias equipes e várias etapas no mesmo culto.
- Duplicação de culto/cronograma e uso de templates para velocidade operacional.
- Histórico e auditoria obrigatórios (criação/alteração/usuário responsável).
- Exclusão de dados críticos com confirmação e política de soft delete quando aplicável.

### Diretrizes de usabilidade
- Linguagem em **pt-BR**, textos simples e orientados a ação.
- Poucos botões por tela, foco visual e hierarquia clara.
- Badges/status coloridos consistentes.
- Feedback de sucesso/erro amigável.
- Layout responsivo mobile-first para operação no dia do culto.

---

## 2) Arquitetura da solução

### Stack
- **Backend**: ASP.NET Core 3.1 + EF Core + MySQL.
- **Frontend**: Angular + Nebular + formulários reativos.
- **Auth**: JWT + login Google (OAuth2).

### Arquitetura em camadas (backend)
- **Domain**: entidades, enums, regras de domínio, contratos.
- **Application**: casos de uso, DTOs, serviços, validações, mapeamentos.
- **Infrastructure**: EF Core (DbContext/mapeamentos), repositórios, provedores externos (JWT/Google), logging.
- **API**: controllers, filtros, middlewares (erro global), documentação e autenticação.

### Padrões técnicos
- Repository + Unit of Work.
- DTO para entrada/saída.
- AutoMapper (ou mapeamento explícito).
- FluentValidation (ou validações internas nos serviços).
- Soft delete em entidades críticas (quando aplicável).
- Auditoria por middleware/interceptor.

### Fluxo de autenticação
1. Usuário faz login (e-mail/senha) ou Google.
2. API valida credenciais e perfil.
3. API emite JWT com claims (`sub`, `email`, `role`, `name`).
4. Frontend guarda token (localStorage/sessionStorage conforme “manter conectado”).
5. Interceptor injeta `Authorization: Bearer` nas chamadas.
6. Guards bloqueiam rotas por perfil.

---

## 3) Estrutura sugerida de pastas

## Backend (`/backend`)

```text
backend/
  src/
    GestaoCulto.Domain/
      Entities/
      Enums/
      Interfaces/
      ValueObjects/
    GestaoCulto.Application/
      DTOs/
      Interfaces/
      Services/
      Validators/
      Mappings/
    GestaoCulto.Infrastructure/
      Persistence/
        Configurations/
        Repositories/
        Seeds/
      Security/
      ExternalAuth/
      Logging/
    GestaoCulto.API/
      Controllers/
      Middleware/
      Filters/
      Extensions/
      Models/
      appsettings.json
  tests/
    GestaoCulto.UnitTests/
    GestaoCulto.IntegrationTests/
```

## Frontend (`/frontend`)

```text
frontend/
  src/
    app/
      core/
        guards/
        interceptors/
        services/
        models/
      shared/
        components/
        directives/
        pipes/
        utils/
      modules/
        auth/
        dashboard/
        cultos/
        templates-culto/
        cronograma/
        escalas/
        convidados/
        voluntarios/
        ministerios/
        checklist/
        relatorios/
      layout/
        menu/
        header/
        footer/
      app-routing.module.ts
      app.module.ts
    assets/
      i18n/
      icons/
      images/
```

---

## 4) Modelagem de dados (visão lógica)

### Núcleo de identidade e acesso
- `usuario`
- `perfil`
- `usuario_perfil` (N:N)
- `usuario_google` (1:1 opcional com `usuario`)

### Núcleo ministerial e pessoas
- `ministerio`
- `usuario_ministerio_lider` (N:N para líderes)
- `voluntario`
- `voluntario_ministerio` (N:N voluntário-equipe)

### Núcleo de culto e planejamento
- `status_culto`
- `culto`
- `template_culto`
- `template_etapa_culto`
- `template_checklist_item`
- `status_etapa`
- `etapa_culto`

### Núcleo de execução e operação
- `presenca_escala_status`
- `escala`
- `checklist`
- `checklist_item`

### Núcleo de recepção e acompanhamento
- `convidado`
- `novo_convertido`

### Governança
- `auditoria`

### Relacionamentos-chave
- `culto` 1:N `etapa_culto`
- `culto` 1:N `escala`
- `etapa_culto` 1:N `escala`
- `voluntario` N:N `ministerio`
- `template_culto` 1:N `template_etapa_culto`
- `template_culto` 1:N `template_checklist_item`
- `culto` 1:N `checklist`
- `checklist` 1:N `checklist_item`
- `convidado` 1:0..1 `novo_convertido`

---

## 5) Entregáveis desta etapa

- Arquitetura funcional e técnica definida.
- Estrutura de pastas sugerida para backend/frontend.
- Modelagem lógica consolidada.
- Script SQL inicial em `database/schema-inicial.sql` com:
  - tabelas, PK/FK, índices
  - seeds de perfis, ministérios e status
  - base preparada para EF Core migrations futuras
