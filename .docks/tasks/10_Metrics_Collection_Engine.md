# Task 10: Motor de Coleta de Métricas em Tempo Real

## Objetivo
Coletar dados de telemetria do Docker e salvar no banco de métricas para cálculo financeiro e exibição nos painéis.

## Requisitos

### 1. MetricsCollectionWorker (BackgroundService)
- Roda a cada 60 segundos.
- Chama a API de Stats do Docker local (`Stream = false`) via `Docker.DotNet` ou fallback.
- Filtra apenas containers com label `cloudios.managed=true`.

### 2. Batch Insert
- Acumular todas as métricas do ciclo em memória.
- Inserir em uma **única transação** no `cloudios_metrics.db` (via `MetricsDbContext`).
- Isto evita contenção de escrita no SQLite.

### 3. Dados Coletados por Container
- `CpuPercent` — % de CPU usada
- `MemoryUsedBytes` — RAM física usada
- `NetworkRxBytes` — Bytes recebidos
- `NetworkTxBytes` — Bytes enviados
- `BlockReadBytes` — I/O de leitura em disco
- `BlockWriteBytes` — I/O de escrita em disco

### 4. Limpeza de Histórico
Criar `MetricsCleanupWorker` (BackgroundService) que:
- Roda diariamente às 03:00 UTC
- Deleta registros de `ContainerMetrics_History` mais velhos que 90 dias
- Executa `PRAGMA optimize;` após a limpeza

### 5. Endpoint de Métricas
Implementar endpoints conforme `API_CONTRACT.md`:
- `GET /api/containers/{id}/metrics` — Métricas históricas de um container (com filtro de data e intervalo)
- `GET /api/metrics/host` — Métricas agregadas do host físico (GlobalAdmin apenas)

### 6. JsonSerializerContext
Criar `MetricsJsonContext` com DTOs de métricas.

## Critérios de Aceite
* O banco `cloudios_metrics.db` começa a preencher automaticamente com containers ligados.
* Batch insert funciona — uma transação por ciclo, não uma por container.
* Limpeza diária remove registros com mais de 90 dias.
