# Task 11: Motor de Bilhetagem e Cálculo Financeiro

## Objetivo
Transformar as métricas de tempo de execução dos containers em valores em Reais (BRL), calculando o custo acumulado por container, por Realm e global.

## Requisitos

### 1. Lógica de Preço
- Cada container possui `CostPerHourBRL` definido no momento do deploy (Task 07).
- O custo é proporcional ao tempo em que o container ficou efetivamente rodando.

### 2. BillingService
Criar `IBillingService` com métodos:
- `RegisterStartAsync(containerId, startedAtUtc)` — Registra início de cobrança (chamado pelo handler de `ContainerStartedEvent`)
- `RegisterStopAsync(containerId, stoppedAtUtc)` — Registra fim de cobrança e calcula horas (chamado pelo handler de `ContainerStoppedEvent`)
- `GetRealmBillingAsync(realmId, month)` — Retorna faturamento do Realm no mês
- `GetGlobalBillingAsync(month)` — Retorna faturamento global de todos os Realms

### 3. Cálculo de Horas
- Usar o campo `StartedAtUtc` do container como referência.
- Quando o container para, calcular: `hours = (stoppedAtUtc - startedAtUtc).TotalHours`
- Custo do período: `hours * costPerHourBRL`
- Para containers rodando no momento da consulta: `hours = (DateTimeOffset.UtcNow - startedAtUtc).TotalHours`

### 4. Endpoints de Billing
Implementar conforme `API_CONTRACT.md`:
- `GET /api/billing/realm` — Custo acumulado do mês atual para o Realm do JWT
- `GET /api/billing/global` — Faturamento geral (GlobalAdmin apenas)

### 5. Campo Calculado no Container DTO
Na listagem de containers (Task 07), o campo `currentMonthCostBRL` deve ser calculado sob demanda:
- Somar todas as horas de vida do container no mês atual × `costPerHourBRL`

### 6. JsonSerializerContext
Criar `BillingJsonContext` com DTOs de billing.

## Critérios de Aceite
* Container ligado há 10 horas com taxa de R$ 0,02/hora = R$ 0,20.
* Billing global soma corretamente todos os Realms.
* Parar e reiniciar um container acumula o custo corretamente (não reseta).
