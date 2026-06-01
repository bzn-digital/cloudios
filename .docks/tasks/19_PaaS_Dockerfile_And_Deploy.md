# Task 19: Dockerfile da PaaS e Deploy do Cloudios

## Objetivo
Containerizar o próprio Cloudios e criar o fluxo de build e deploy da aplicação, permitindo que ela rode como um container Docker no host.

## Requisitos

### 1. Dockerfile Multi-Stage
Criar `Dockerfile` na raiz do repositório:

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Bzn.Cloudios.sln .
COPY src/ ./src/
RUN dotnet restore
RUN dotnet publish src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj \
    -c Release -r linux-x64 -o /app/publish

# Stage 2: Build WASM
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS wasm-build
WORKDIR /src
COPY --from=build /src/ ./src/
RUN dotnet publish src/Bzn.Cloudios.WebApp/Bzn.Cloudios.WebApp.csproj \
    -c Release -o /app/wasm

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=wasm-build /app/wasm/wwwroot ./wwwroot
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["./Bzn.Cloudios.WebAPI"]
```

### 2. Docker Compose (Desenvolvimento)
Criar `docker-compose.dev.yml` para ambiente local de desenvolvimento:
- Serviço `cloudios-api` com o Dockerfile acima
- Mount do Docker socket
- Variáveis de ambiente de desenvolvimento
- Serviço `cloudflared` para teste de tunnel local

### 3. Docker Compose (Produção)
Criar `docker-compose.yml` para produção conforme `DEPLOYMENT_TOPOLOGY.md`:
- Serviço `cloudios` com imagem buildada
- Porta 8080 apenas em localhost
- Volumes para dados persistentes e Docker socket
- Variáveis de ambiente de produção
- Restart policy `unless-stopped`

### 4. .dockerignore
Criar `.dockerignore` para excluir:
```
bin/
obj/
.git/
.github/
.docks/
*.md
.env
```

### 5. Script de Build
Criar `scripts/build.sh` (ou `build.ps1` para Windows dev):
- Build da imagem Docker
- Tag com versão do assembly
- Push para registry (se configurado)

## Critérios de Aceite
* `docker build -t bzn/cloudios:latest .` gera a imagem sem erros.
* `docker compose up` sobe o Cloudios com acesso ao Docker socket.
* A WebAPI responde em `http://localhost:8080/health`.
* O Blazor WASM é servido corretamente pela WebAPI.
