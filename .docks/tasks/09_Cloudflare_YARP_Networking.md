# Task 09: Redes, YARP Proxy e Cloudflare Tunnels

## Objetivo
Configurar o YARP como reverse proxy embutido na WebAPI, com rotas dinâmicas atualizadas em tempo real quando containers sobem ou descem, integrando com o Cloudflare Tunnel.

## Requisitos

### 1. YARP Configuration
- Registrar `AddReverseProxy()` no DI do WebAPI.
- Configurar rotas iniciais vazias — tudo será dinâmico via código.

### 2. IYarpRouteUpdater
Criar serviço que manipula as rotas do YARP em tempo real:
- `AddRouteAsync(containerId, internalIp, internalPort, hostname)` — Adiciona rota: `hostname → http://{internalIp}:{internalPort}`
- `RemoveRouteAsync(containerId)` — Remove a rota do container
- Usar `IProxyStateLookup` ou manipulação programática do `InMemoryConfigProvider` do YARP (AOT-safe, sem recarregamento de config JSON)

### 3. Assinatura de Eventos
O `YarpRouteUpdater` assina eventos do `IEventBus`:
- `ContainerStartedEvent` → `AddRouteAsync`
- `ContainerStoppedEvent` → `RemoveRouteAsync`
- `ContainerDeletedEvent` → `RemoveRouteAsync`

### 4. Roteamento de Request
A lógica de roteamento no WebAPI deve ser:
1. Se o hostname corresponde a um container gerenciado → YARP proxy
2. Se a rota começa com `/api/*` → Minimal API handler
3. Se a rota é `/health` → Health check
4. Se é arquivo estático → Blazor WASM
5. Fallback → `index.html` (Blazor WASM SPA)

### 5. Forwarded Headers
Configurar `ForwardedHeadersOptions` para aceitar cabeçalhos do Cloudflared Tunnel:
```csharp
options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
options.KnownNetworks.Clear();
options.KnownProxies.Clear();
```
Chamar `app.UseForwardedHeaders()` antes de `app.UseRouting()`.

### 6. Hostname Pattern
Cada container recebe um hostname no formato: `{container-name}.{realm-slug}.cloudios.bzn.dev`
- Exemplo: `web-api.acme.cloudios.bzn.dev`
- O Cloudflare Tunnel deve ter wildcard configurado: `*.cloudios.bzn.dev → localhost:8080`

## Critérios de Aceite
* Requisição HTTP para `web-api.acme.cloudios.bzn.dev` chega pelo túnel, bate no YARP e é direcionada ao container correto.
* Quando o container para, a rota é removida e o YARP retorna 502 ou 404.
* `dotnet publish -c Release -r linux-x64` compila sem erros.
