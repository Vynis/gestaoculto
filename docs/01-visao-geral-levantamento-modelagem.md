# Gestão de Culto - Visão Geral, Funcional e Modelagem

## 1. Visão geral da solução

Sistema web para planejamento e operação de cultos, orientado a velocidade no dia do evento e simplicidade para usuários com baixa familiaridade tecnológica.

### Objetivos de produto
- Montar culto e escala em poucos minutos (com template e duplicação).
- Executar o culto com tela objetiva em tempo real (operação ao vivo).
- Registrar convidados e novos convertidos em fluxo curto (poucos campos).
- Garantir histórico e rastreabilidade (auditoria e carimbos de usuário/data).

### Decisões de arquitetura (e por que)
- **Arquitetura em camadas (Domain/Application/Infrastructure/API)**: reduz acoplamento e facilita evolução futura sem quebrar tudo.
- **RBAC por perfil**: regra simples de autorização para operação diária (admin, gestão, líder, voluntário, recepção/dados).
- **Status em tabelas dedicadas** (`status_culto`, `status_etapa`, `presenca_escala_status`): padroniza cores, filtros e relatórios.
- **Template + duplicação**: acelera montagem de novos cultos com mínimo esforço.
- **Auditoria central**: mantém governança sem exigir operação manual da equipe técnica.
- **Soft delete para dados críticos** (nível de aplicação): evita perda acidental e mantém histórico.

### Compatibilidade garantida
- Backend: ASP.NET Core 3.1 + C# + EF Core 3.1.
- Banco: MySQL/MariaDB 10.2.36.
- Frontend: Angular + Nebular (estrutura modular, formulários reativos).

---

## 2. Levantamento funcional detalhado

## 2.1 Perfis e escopo
- **Administrador**: usuários, perfis, parâmetros, visão completa e edição completa.
- **Gestão de Culto**: cria/edita cultos, cronograma, escalas, checklists e operação.
- **Líder de Ministério**: confirma presença e execução da própria equipe.
- **Voluntário**: consulta escala pessoal, confirma presença e conclui tarefas permitidas.
- **Recepção / Dados**: registra convidados e novos convertidos, acompanha pendências.

## 2.2 Módulos e casos de uso

### A) Autenticação e sessão
- Login por e-mail/senha.
- Login com Google.
- Recuperar senha.
- Manter conectado (persistência de sessão).
- Mensagens amigáveis de erro/sucesso.

### B) Dashboard
- Próximo culto (data, horário, status).
- Total de voluntários escalados.
- Tarefas/checklists pendentes.
- Convidados e novos convertidos do culto.
- Alertas operacionais (atrasos/incidentes).

### C) Cultos
- Criar/editar culto.
- Duplicar culto anterior.
- Criar culto por template.
- Estados: planejamento, fechado, em andamento, finalizado, cancelado.

### D) Templates
- Criar/editar/duplicar template.
- Definir etapas padrão e equipes padrão por etapa.
- Definir checklist padrão.
- Converter culto existente em template.

### E) Cronograma
- Cadastro de etapas com sequência visual.
- Cálculo automático de horário de término por duração.
- Exibir atraso e progresso.
- Modos timeline e lista simples.
- Impressão para uso offline.

### F) Ministérios e voluntários
- Cadastro de ministérios/equipes.
- Definição de líderes.
- Cadastro de voluntários (principal/secundário, restrições, ativo/inativo).

### G) Escalas e presença
- Escala por culto e por etapa.
- Múltiplos voluntários na mesma atividade.
- Status de presença: pendente, confirmado, ausente, substituído.
- Registro de substituição e observações.

### H) Convidados e novos convertidos
- Cadastro em poucos segundos (nome, telefone, convidante, primeira vez, observação).
- Marcação de novo convertido e status de acompanhamento.
- Exportação para acompanhamento pastoral.

### I) Checklist operacional
- Listas por fase (pré/durante/pós).
- Itens por ministério.
- Estados: pendente, concluído, não se aplica.

### J) Relatórios
- Cultos realizados.
- Escala por voluntário.
- Presença e faltas.
- Convidados e novos convertidos.
- Consolidação por ministério.
- Exportação PDF e Excel.

## 2.3 Regras de negócio
- Cada culto possui múltiplas etapas ordenadas por sequência.
- Cada etapa pode ter equipe responsável e responsável principal.
- Voluntário pode participar de vários ministérios e várias etapas no mesmo culto.
- Escala pode existir com ou sem vínculo direto à etapa (atividade geral do culto).
- Exclusão de dados críticos exige confirmação explícita e registro em auditoria.
- Toda alteração relevante grava `criado_em`, `atualizado_em`, `criado_por`, `atualizado_por`.

## 2.4 Requisitos de UX para usuários leigos
- Poucos campos por tela e labels claras em pt-BR.
- Uso de cores para status com legenda simples.
- Ações principais em destaque e com feedback visual imediato.
- Evitar excesso de informação na mesma tela (resumo + detalhe progressivo).
- Responsividade para celular (principalmente recepção).

---

## 3. Modelagem de entidades e relacionamentos

## 3.1 Entidades principais
- `usuario`, `perfil`, `usuario_perfil`, `usuario_google`
- `ministerio`, `usuario_ministerio_lider`, `voluntario`, `voluntario_ministerio`
- `status_culto`, `culto`, `template_culto`, `template_etapa_culto`, `template_checklist_item`
- `status_etapa`, `etapa_culto`
- `presenca_escala_status`, `escala`, `presenca_escala`
- `checklist`, `checklist_item`
- `convidado`, `novo_convertido`
- `auditoria`

## 3.2 Cardinalidade (resumo)
- `usuario` N:N `perfil` (via `usuario_perfil`).
- `usuario` 1:0..1 `usuario_google`.
- `voluntario` N:N `ministerio` (via `voluntario_ministerio`).
- `culto` 1:N `etapa_culto`.
- `culto` 1:N `escala`; `etapa_culto` 1:N `escala`.
- `escala` 1:N `presenca_escala` (histórico de confirmações).
- `culto` 1:N `checklist`; `checklist` 1:N `checklist_item`.
- `culto` 1:N `convidado`; `convidado` 1:0..1 `novo_convertido`.

## 3.3 Decisões de modelagem (e por que)
- **`escala.presenca_status_id` + `presenca_escala`**: status atual rápido para listagem e histórico detalhado para auditoria operacional.
- **Tabelas de status separadas**: permite alterar nomes/cores/ordem sem mexer em código.
- **`culto_origem_id`**: rastreia duplicação e facilita análises futuras.
- **Campos de auditoria em entidades críticas**: conformidade operacional e responsabilização.
- **Índices por data/status/relacionamento**: melhora desempenho dos módulos mais usados (dashboard, cronograma, escala, relatórios).

---

## 4. Arquitetura de pastas

## 4.1 Backend

```text
backend/
  src/
    GestaoCulto.Domain/
      Entities/
      Enums/
      Interfaces/
      Exceptions/
    GestaoCulto.Application/
      DTOs/
        Auth/
        Cultos/
        Escalas/
        Convidados/
      Interfaces/
      Services/
      Validators/
      Mappings/
    GestaoCulto.Infrastructure/
      Persistence/
        Context/
        Configurations/
        Repositories/
        Seeds/
      Security/
        Jwt/
        Password/
      ExternalAuth/
        Google/
      Logging/
    GestaoCulto.API/
      Controllers/
      Middleware/
      Filters/
      Extensions/
      Swagger/
      appsettings.json
      appsettings.Development.json
  tests/
    GestaoCulto.UnitTests/
    GestaoCulto.IntegrationTests/
```

## 4.2 Frontend

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
        pipes/
        directives/
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
      app-routing.module.ts
      app.module.ts
```

### Organização funcional (decisão)
- Módulos por domínio de negócio, não por tipo técnico, para facilitar manutenção por equipe pequena.
- `core` concentra autenticação, sessão e integração API.
- `shared` contém componentes reutilizáveis (cards de status, tabela simples, badges, modais de confirmação).

---

## 5. Script inicial do banco

Script pronto e coerente com a modelagem em:

- `database/schema-inicial.sql`

Conteúdo incluído:
- Criação de banco/tabelas com PK/FK.
- Índices para consulta frequente.
- Tabelas de domínio de status.
- Tabela de auditoria.
- Seeds iniciais para perfis, ministérios e status.
- Compatibilidade com MySQL/MariaDB 10.2.36.
