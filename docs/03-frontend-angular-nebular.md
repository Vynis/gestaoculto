# Gestão de Culto - Etapa 3 (Frontend Angular + Nebular)

## Entrega realizada

Frontend funcional em Angular com Nebular, organizado por módulos e com foco em simplicidade para usuários leigos.

## O que foi implementado

- Estrutura de layout com menu lateral e cabeçalho simples.
- Login com e-mail/senha e botão de login Google.
- Guard e interceptor para autenticação com JWT.
- Dashboard com visão resumida dos indicadores principais.
- Telas funcionais de Cultos, Cronograma, Escalas e Convidados.
- Tela de Templates de Culto para reaproveitamento rápido.
- Textos em pt-BR, poucos botões por tela e formulários objetivos.

## Arquivos principais

- `frontend/src/app/app.module.ts`
- `frontend/src/app/app-routing.module.ts`
- `frontend/src/app/layout/main-layout.component.ts`
- `frontend/src/app/modules/auth/login/login.component.ts`
- `frontend/src/app/modules/dashboard/dashboard.component.ts`
- `frontend/src/app/modules/cultos/cultos.component.ts`
- `frontend/src/app/modules/cronograma/cronograma.component.ts`
- `frontend/src/app/modules/escalas/escalas.component.ts`
- `frontend/src/app/modules/convidados/convidados.component.ts`
- `frontend/src/app/modules/templates-culto/templates-culto.component.ts`
- `frontend/src/app/core/services/*.ts`
- `frontend/src/app/core/guards/auth.guard.ts`
- `frontend/src/app/core/interceptors/auth.interceptor.ts`

## Integração com backend

- URL base da API: `frontend/src/environments/environment.ts`
- Padrão de autenticação: token Bearer no interceptor.
- Fluxo de sessão: persistência no `localStorage`.

## Observação de ambiente

Para build local com Angular CLI, use Node.js compatível com o projeto (>= 16.13 para Angular 16).
