# Task 13: Container Logs Viewer

## Objetivo
Permitir que clientes e admins visualizem os logs de saída (stdout/stderr) dos containers Docker em tempo real e sob demanda.

## Requisitos

### 1. Endpoint de Logs
Implementar conforme `API_CONTRACT.md`:
- `GET /api/containers/{id}/logs?tail=100` — Últimas N linhas de logs do container

### 2. Implementação
- Usar `Docker.DotNet` para chamar `ContainerLogsAsync` com `stdout=true, stderr=true, timestamps=true, tail=100`.
- Parsear o output do Docker (formato: `8-byte header + payload`, onde o header indica stream type: 1=stdout, 2=stderr).
- Retornar como lista de `LogEntry` com `timestamp`, `stream` e `line`.

### 3. Autorização
- Qualquer role do Realm do container pode ver os logs.
- `GlobalAdmin` pode ver logs de qualquer container.

### 4. Tratamento de Container Parado
- Se o container está parado, retornar os logs disponíveis do Docker (última execução).
- Se o container nunca rodou, retornar lista vazia.

### 5. Streaming (Futuro — não implementar agora)
- Reservar o endpoint `GET /api/containers/{id}/logs/stream` para Server-Sent Events em tempo real.
- Não implementar nesta task — apenas deixar o placeholder documentado.

### 6. JsonSerializerContext
Adicionar `LogEntry` ao `ContainerJsonContext`.

## Critérios de Aceite
* `GET /api/containers/{id}/logs?tail=50` retorna as últimas 50 linhas de log do container.
* Logs distinguem stdout de stderr.
* Container parado retorna logs da última execução.
