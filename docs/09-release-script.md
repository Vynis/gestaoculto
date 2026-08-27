# Release automatizado com `release.ps1`

Este projeto possui um script único de release na raiz: `release.ps1`.

Ele prepara frontend e backend para publicação, com artefatos em `publish/`.

## O que o script faz

1. Atualiza o arquivo `VERSION` (se você passar `-Version`)
2. Gera metadados de versão do frontend (`npm run prepare:version`)
3. Gera build do frontend para produção (KingHost)
4. Publica o backend em modo Release
5. Ajusta o `web.config` publicado para compatibilidade da KingHost (`modules="AspNetCoreModule"`)
6. Cria `version.json` no backend publicado
7. Gera zip dos pacotes (`frontend.zip` e `backend.zip`), a menos que use `-SkipZip`

## Pré-requisitos

- Node.js instalado
- Dependências do frontend instaladas (`frontend/node_modules`)
- .NET SDK instalado
- Executar o comando na raiz do repositório

## Comandos de uso

### Release padrão (com zip)

```powershell
powershell -ExecutionPolicy Bypass -File .\release.ps1
```

### Release com nova versão

```powershell
powershell -ExecutionPolicy Bypass -File .\release.ps1 -Version 0.1.1
```

### Release sem gerar zip

```powershell
powershell -ExecutionPolicy Bypass -File .\release.ps1 -SkipZip
```

## Saídas geradas

- Frontend publicado: `publish/frontend`
- Backend publicado: `publish/backend`
- Zip frontend: `publish/frontend.zip`
- Zip backend: `publish/backend.zip`

## Observações

- O build de produção do frontend usa `baseHref` configurado para `"/gestaoculto/"`.
- A API está configurada para operar com base path `"/gestaoculto"` em produção.
- A pasta `publish/` está no `.gitignore` e não deve ser versionada.
