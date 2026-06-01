# Deployment Topology — BZN Cloudios

> Este documento descreve a infraestrutura física e lógica do Cloudios em produção, incluindo o fluxo de rede, configuração do Docker e variáveis de ambiente.

---

## 1. Diagrama de Topologia

```
INTERNET
  │
  ▼
┌─────────────────────────────┐
│      CLOUDFLARE EDGE        │
│  (CDN, DDoS Protection,    │
│   SSL Termination)          │
└─────────────┬───────────────┘
              │ HTTPS
              ▼
┌─────────────────────────────┐
│    CLOUDFLARED TUNNEL        │
│  (cloudflared daemon)        │
│  Runs on host Linux server   │
│  Connects outbound to CF     │
└─────────────┬───────────────┘
              │ HTTP (localhost:8080)
              ▼
┌─────────────────────────────────────────────────────┐
│                  HOST LINUX SERVER                    │
│                                                       │
│  ┌──────────────────────────────────────────────┐   │
│  │  CLOUDIOS WEBAPI (Native AOT Binary)          │   │
│  │  - Minimal APIs (port 8080)                   │   │
│  │  - YARP Reverse Proxy (in-process)            │   │
│  │  - Serves Blazor WASM static files            │   │
│  │  - SQLite databases (cloudios_main.db,        │   │
│  │    cloudios_metrics.db)                        │   │
│  │  - Docker socket mount:                       │   │
│  │    /var/run/docker.sock                        │   │
│  └──────────┬───────────────────────────────────┘   │
│             │ docker.sock                             │
│             ▼                                         │
│  ┌──────────────────────────────────────────────┐   │
│  │  DOCKER DAEMON                                │   │
│  │  - Manages customer containers                │   │
│  │  - Docker network: cloudios_internal          │   │
│  │    (172.20.0.0/16)                            │   │
│  └──────────┬───────────────────────────────────┘   │
│             │                                         │
│    ┌────────┴────────┐                              │
│    ▼                  ▼                              │
│  ┌──────────┐  ┌──────────┐                         │
│  │ Container │  │ Container │  (N containers)       │
│  │ Realm A   │  │ Realm B   │                        │
│  │ 172.20.0.2│  │ 172.20.0.3│                        │
│  └──────────┘  └──────────┘                         │
└─────────────────────────────────────────────────────┘
```

---

## 2. Fluxo de Rede — Request Externo

```
1. Usuário acessa: https://app.acme.cloudios.bzn.dev
2. Cloudflare Edge recebe a requisição HTTPS
3. Cloudflare roteia pelo Tunnel configurado para o hostname
4. cloudflared no host encaminha para http://localhost:8080
5. Cloudios WebAPI recebe a requisição:
   a. Se a rota começa com /api/* → Minimal API handler
   b. Se a rota é /health → Health check handler
   c. Se o hostname corresponde a um container → YARP proxy
   d. Se é rota de arquivo estático → Blazor WASM files
   e. Fallback → index.html (Blazor WASM SPA)
6. YARP encaminha para o container no Docker network interno:
   http://172.20.0.{n}:{port}
7. Response volta pelo mesmo caminho (YARP → cloudflared → CF → Usuário)
```

---

## 3. Como o Cloudios Roda

### 3.1 Opção A: Diretamente no Host (Recomendado para VPS de baixo custo)

```bash
# O binário AOT roda diretamente no Linux
./Bzn.Cloudios.WebAPI
```

- Sem overhead de container para o próprio Cloudios
- Acesso direto ao `/var/run/docker.sock`
- Menor consumo de RAM

### 3.2 Opção B: Docker com Socket Mount

```yaml
# docker-compose.yml
services:
  cloudios:
    image: bzn/cloudios:latest
    ports:
      - "127.0.0.1:8080:8080"   # Apenas localhost, sem exposição externa
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - cloudios_data:/app/data
      - /var/lib/cloudios/volumes:/var/lib/cloudios/volumes  # Host volumes para containers clientes
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ADMIN_EMAIL=${ADMIN_EMAIL}
      - ADMIN_PASSWORD=${ADMIN_PASSWORD}
    restart: unless-stopped

volumes:
  cloudios_data:
```

**Nota:** O socket Docker é montado como **read-only** (`:ro`) para segurança. O Cloudios só precisa ler estados e enviar comandos; não precisa escrever no socket.

---

## 4. Docker Network Interna

O Cloudios cria uma rede Docker interna para isolar os containers dos clientes:

```bash
docker network create --driver bridge --subnet 172.20.0.0/16 cloudios_internal
```

Todos os containers de clientes são criados nesta rede. O YARP roteia para os IPs desta rede.

---

## 5. Variáveis de Ambiente

| Variável | Obrigatória | Descrição | Exemplo |
|----------|-------------|-----------|---------|
| `ASPNETCORE_ENVIRONMENT` | Sim | Ambiente de execução | `Production` |
| `ASPNETCORE_URLS` | Sim | Bind address | `http://0.0.0.0:8080` |
| `ADMIN_EMAIL` | Sim | Email do GlobalAdmin inicial | `admin@bzn.dev` |
| `ADMIN_PASSWORD` | Sim | Senha do GlobalAdmin inicial | `secure-password` |
| `CLOUDIOS_DATA_DIR` | Não | Diretório dos bancos SQLite | `/var/lib/cloudios/data` |
| `CLOUDFLARE_TUNNEL_TOKEN` | Sim | Token do Cloudflare Tunnel | `tunnel-token-here` |
| `JWT_SECRET_KEY` | Sim | Chave de assinatura JWT (min 32 chars) | `base64-encoded-key` |
| `JWT_EXPIRY_HOURS` | Não | Validade do token JWT | `24` (padrão) |

---

## 6. Configuração do Cloudflared

### 6.1 Instalação

```bash
# Debian/Ubuntu
curl -L https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -o cloudflared.deb
sudo dpkg -i cloudflared.deb
```

### 6.2 Configuração do Tunnel

```bash
cloudflared tunnel login
cloudflared tunnel create cloudios
```

### 6.3 config.yml

```yaml
tunnel: cloudios
credentials-file: /etc/cloudflared/credentials.json

ingress:
  # Painel Cloudios (Blazor WASM)
  - hostname: cloudios.bzn.dev
    service: http://localhost:8080

  # Containers de clientes — wildcard
  - hostname: "*.cloudios.bzn.dev"
    service: http://localhost:8080

  # Regra catch-all
  - service: http_status:404
```

### 6.4 Systemd Service

```ini
# /etc/systemd/system/cloudflared.service
[Unit]
Description=Cloudflare Tunnel
After=network.target

[Service]
ExecStart=/usr/bin/cloudflared tunnel run cloudios
Restart=always
Environment=TUNNEL_TOKEN=%i

[Install]
WantedBy=multi-user.target
```

---

## 7. Forwarded Headers

O Cloudios WebAPI deve confiar nos headers do Cloudflared para obter o IP real do cliente:

```csharp
// Program.cs
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Antes de app.UseRouting()
app.UseForwardedHeaders();
```

---

## 8. Diretórios no Host

```
/var/lib/cloudios/
├── data/                          # Bancos SQLite
│   ├── cloudios_main.db
│   └── cloudios_metrics.db
├── volumes/                       # Volumes dos containers clientes
│   ├── realm-{guid}/
│   │   ├── container-{guid}/
│   │   │   └── data/
│   │   └── ...
│   └── ...
└── logs/                          # Logs do Cloudios (se configurado)
```

---

## 9. Portas

| Porta | Serviço | Exposta Externamente? |
|-------|---------|----------------------|
| 8080 | Cloudios WebAPI | **NÃO** — apenas localhost |
| - | Cloudflared | **NÃO** — outbound only |
| - | Docker containers | **NÃO** — rede interna 172.20.0.0/16 |

**Nenhuma porta é exposta na internet.** Todo tráfego passa pelo Cloudflare Tunnel.
