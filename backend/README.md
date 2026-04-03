# Backend - Gestão de Culto

API em ASP.NET Core 3.1 com arquitetura em camadas.

## Pré-requisitos

- .NET SDK com suporte a `netcoreapp3.1`
- MySQL/MariaDB 10.2+

## Execução

```bash
dotnet restore GestaoCulto.sln
dotnet build GestaoCulto.sln
dotnet run --project src/GestaoCulto.API/GestaoCulto.API.csproj
```

Swagger:

- `http://localhost:5000/docs`

## Configuração

Arquivo: `src/GestaoCulto.API/appsettings.Development.json`

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `GoogleAuth:ClientId`

## Usuário inicial

- E-mail: `admin@gestaoculto.local`
- Senha: `Admin@123`
