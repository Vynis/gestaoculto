# Gestão de Culto - Etapa 2 (Backend ASP.NET Core 3.1)

## Entrega realizada

Backend funcional em C# com ASP.NET Core 3.1, organizado em camadas e conectado ao modelo MySQL planejado.

## Estrutura criada

```text
backend/
  GestaoCulto.sln
  src/
    GestaoCulto.Domain/
    GestaoCulto.Application/
    GestaoCulto.Infrastructure/
    GestaoCulto.API/
```

## Decisões técnicas aplicadas

- Camadas separadas para facilitar manutenção e evolução futura.
- JWT com claims de perfil para autorização por rota.
- Login local e login Google no mesmo fluxo de autenticação.
- Seed inicial automático para perfis, status, ministérios e usuário admin.
- Controllers com mensagens simples em pt-BR para facilitar consumo no frontend.

## Recursos implementados

### Autenticação
- `POST /api/auth/login`
- `POST /api/auth/google`

### Dashboard
- `GET /api/dashboard/resumo`

### Cultos (CRUD principal)
- `GET /api/cultos`
- `GET /api/cultos/{id}`
- `POST /api/cultos`
- `PUT /api/cultos/{id}`
- `DELETE /api/cultos/{id}`

### Cronograma (etapas)
- `GET /api/cronograma/culto/{cultoId}`
- `POST /api/cronograma`

### Escalas
- `GET /api/escalas/culto/{cultoId}`
- `POST /api/escalas`
- `POST /api/escalas/{id}/confirmar`

### Cadastros principais
- `GET /api/ministerios`
- `POST /api/ministerios`
- `GET /api/voluntarios`
- `POST /api/voluntarios`
- `GET /api/convidados`
- `POST /api/convidados`

## Configurações

Arquivo: `backend/src/GestaoCulto.API/appsettings.Development.json`

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
- `GoogleAuth:ClientId`
- `Cors:AllowedOrigins`

## Credencial inicial (seed)

- E-mail: `admin@gestaoculto.local`
- Senha: `Admin@123`

> Altere essa credencial em ambiente real.

## Como executar

1. Subir MySQL/MariaDB e garantir que o banco `gestao_culto` exista.
2. Ajustar string de conexão em `appsettings.Development.json`.
3. Executar:

```bash
dotnet restore backend/GestaoCulto.sln
dotnet build backend/GestaoCulto.sln
dotnet run --project backend/src/GestaoCulto.API/GestaoCulto.API.csproj
```

4. Abrir Swagger em `/docs`.
