# Task 19: Dockerfile da PaaS e Deploy do Cloudios (Native AOT)

## Objetivo
Containerizar o próprio Cloudios e criar o fluxo de build e deploy da aplicação. O resultado será uma imagem Docker ultraleve (Native AOT) que serve os dois painéis Blazor WASM e gerencia o Docker do host via socket.

## Requisitos de Implementação

### 1. Dockerfile Multi-Stage (AOT + WASM)
Criar o `Dockerfile` na raiz do repositório garantindo a compilação AOT real:

```dockerfile
# Stage 1: Build Environment (SDK com dependências AOT para Alpine)
FROM [mcr.microsoft.com/dotnet/sdk:10.0-alpine](https://mcr.microsoft.com/dotnet/sdk:10.0-alpine) AS build-env
RUN apk add --no-cache clang build-base zlib-dev
WORKDIR /src

# Copiar arquivos de solução e projetos
COPY . .
RUN dotnet restore Bzn.Cloudios.sln

# Build da API (Native AOT)
RUN dotnet publish src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj \
    -c Release -r linux-musl-x64 --self-contained true -o /app/api

# Build do Frontend do Cliente (WASM)
RUN dotnet publish src/Bzn.Cloudios.WebApp/Bzn.Cloudios.WebApp.csproj \
    -c Release -o /app/webapp

# Build do Frontend Administrativo (WASM)
RUN dotnet publish src/Bzn.Cloudios.WebPlatform/Bzn.Cloudios.WebPlatform.csproj \
    -c Release -o /app/webplatform

# Stage 2: Runtime Ultraleve
FROM alpine:latest
# Dependências mínimas de globalização e rede para o binário AOT
RUN apk add --no-cache libstdc++ icu-libs
WORKDIR /app

# Copiar o binário nativo compilado
COPY --from=build-env /app/api/Bzn.Cloudios.WebAPI .

# Copiar os artefatos estáticos dos dois frontends
# A WebAPI deverá ser configurada para servir essas pastas em rotas distintas (ex: /app e /admin)
COPY --from=build-env /app/webapp/wwwroot ./wwwroot/client
COPY --from=build-env /app/webplatform/wwwroot ./wwwroot/admin

# Configuração de Execução
ENV ASPNETCORE_URLS=[http://0.0.0.0:8080](http://0.0.0.0:8080)
EXPOSE 8080
ENTRYPOINT ["./Bzn.Cloudios.WebAPI"]


### 2. Docker Compose (Produção / Self-Host)
Criar `docker-compose.yml` na raiz preparado para a infraestrutura real com persistência do SQLite:

services:
  cloudios-core:
    image: bzn/cloudios:latest
    container_name: bzn-cloudios
    restart: unless-stopped
    ports:
      - "127.0.0.1:8080:8080" # Exposto apenas para o localhost (Cloudflare Tunnel acessa por aqui)
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock # Sibling Container permission
      - ./cloudios-data:/app/data # Persistência do SQLite (Main e Metrics)
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Database__MainConnection=Data Source=/app/data/cloudios_main.db;Mode=ReadWriteCreate;Cache=Shared;Journal Mode=WAL;
      - Database__MetricsConnection=Data Source=/app/data/cloudios_metrics.db;Mode=ReadWriteCreate;Cache=Shared;Journal Mode=WAL;

  # O serviço do Cloudflared Tunnel deve ser configurado aqui para rotear o tráfego externo para o cloudios-core:8080

### 3. Docker Compose (Desenvolvimento Local)
Criar docker-compose.dev.yml focando em agilidade:

- Fazer o build automático (build: .) ao rodar o compose.
- Mapear o Docker socket.
- Expor na porta 8080 globalmente para testar no navegador local sem o tunnel.

### 4. Arquivos de Suporte
- .dockerignore: Garantir que pastas como bin/, obj/, .git/, e nossa documentação interna .ai/, docs/tasks/ não sejam copiadas para o build context, acelerando a compilação.
- Script de Build: Criar scripts/build.sh contendo o comando padrão docker build -t bzn/cloudios:latest .

### Critérios de Aceite
- O comando docker build finaliza com sucesso e resulta em uma imagem inferior a 150MB (graças ao AOT e Alpine).
- O container do Cloudios sobe via docker compose up e consegue criar um arquivo .db na pasta host mapeada, garantindo a persistência do SQLite.
- A WebAPI AOT inicia corretamente dentro do Alpine e consegue servir os arquivos estáticos dos dois painéis Blazor.