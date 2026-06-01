# Task 14: Health Checks e Monitoramento do Host

## Objetivo
Implementar endpoints de health check para o Cloudflare Tunnel verificar a saúde do Cloudios, e expor métricas do host físico para o painel admin.

## Requisitos

### 1. Health Check Endpoint
- `GET /health` — Público (sem autenticação), para o Cloudflare Tunnel health check.
- Verifica:
  - WebAPI está respondendo
  - Conexão com ambos os SQLite databases funciona
  - Conexão com o Docker socket funciona
- Response conforme `API_CONTRACT.md`:
```json
{
  "status": "Healthy",
  "version": "1.0.0",
  "uptime": "12:30:45"
}
```
- Se qualquer dependência falhar, retornar `"status": "Unhealthy"` com HTTP 503.

### 2. Host Metrics (Admin)
- `GET /api/metrics/host` — GlobalAdmin apenas (já definido na Task 10).
- Coletar dados do host físico:
  - `totalCpuPercent` — via `/proc/stat` (Linux) ou `Docker.SystemInfoAsync()`
  - `totalMemoryUsedBytes` / `totalMemoryTotalBytes` — via `/proc/meminfo` ou `Docker.SystemInfoAsync()`
  - `diskUsedBytes` / `diskTotalBytes` — via `DriveInfo`
  - `activeContainers` — contagem de containers com label `cloudios.managed=true`

### 3. Docker Connectivity Check
- No health check, tentar `DockerClient.System.PingAsync()` para verificar que o socket está acessível.
- Se falhar, reportar `"status": "Degraded"` com detalhes.

### 4. Versionamento
- A versão no health check vem de `Assembly.GetExecutingAssembly().GetName().Version` ou de variável de ambiente `CLOUDIOS_VERSION`.

## Critérios de Aceite
* `GET /health` retorna 200 com status "Healthy" quando tudo está funcionando.
* Se o Docker socket estiver inacessível, retorna 503 com "Degraded" ou "Unhealthy".
* Cloudflare Tunnel pode usar este endpoint para monitorar a saúde do serviço.
