# Task 17: Blazor WASM — Painel Administrativo Global (Dashboard)

## Objetivo
Construir o dashboard global do GlobalAdmin com visão de faturamento total e consumo do host.

## Requisitos

### 1. Dashboard Global
- **Card: Faturamento Geral** — Chama `GET /api/billing/global`, exibe total em R$ do mês atual.
- **Card: Consumo de RAM/CPU do Host** — Chama `GET /api/metrics/host`, exibe barras de progresso com % de uso.
- **Card: Total de Containers Ativos** — Contagem global.
- **Card: Total de Realms** — Contagem de realms ativos.

### 2. Gráficos
- **Gráfico de Faturamento por Realm** — Barras horizontais mostrando custo mensal de cada Realm.
- **Gráfico de Uso do Host** — Linha temporal de CPU/RAM do host (se dados disponíveis).

### 3. Tabela de Faturamento por Realm
- Lista todos os Realms com: Nome, Containers Ativos, Custo Mensal (R$).
- Ordenável por custo (maior primeiro).
- Clicar em um Realm navega para a página de detalhes do Realm (Task 18).

## Critérios de Aceite
* Admin vê faturamento total agregado de todos os clientes.
* Gráficos renderizam corretamente com dados reais da API.
* Dados de CPU/RAM do host refletem o estado real do servidor.
