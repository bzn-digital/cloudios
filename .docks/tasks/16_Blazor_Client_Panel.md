# Task 16: Blazor WASM — Painel do Cliente (Dashboard e Serviços)

## Objetivo
Construir as páginas do painel do cliente: dashboard com custo e métricas, e gerenciador de serviços com ações de container.

## Requisitos

### 1. Dashboard do Cliente
- **Card: Gasto Total do Mês** — Chama `GET /api/billing/realm`, exibe valor em R$ com destaque visual.
- **Gráfico de CPU/RAM** — Chama `GET /api/containers/{id}/metrics` para o container selecionado. Renderizar gráfico de linha simples (usar biblioteca leve como `Blazor.Charts` ou `ChartJs.Blazor` ou SVG customizado).
- **Card: Containers Ativos** — Contagem de containers com status Running.
- **Card: Containers Parados** — Contagem de containers com status Stopped.

### 2. Gerenciador de Serviços (Listagem)
- **Tabela** com os Containers do Realm (chama `GET /api/containers`).
- **Colunas:** Status (badge colorido: verde=Running, amarelo=Stopped, vermelho=Failed), Nome, Imagem, Custo acumulado (R$), CPU Limit, RAM Limit.
- **Campo de Busca** no topo filtrando pelo nome do serviço (query param `search`).
- **Filtro de Status** — Dropdown: All, Running, Stopped, Failed.
- **Paginação** — Controles de página conforme `PagedResponse`.
- **Ações por linha (Botões):** Start, Restart, Stop, Delete.
  - Start e Restart visíveis apenas quando container está Stopped/Failed.
  - Stop visível apenas quando container está Running.
  - Delete com confirmação (modal).
  - Cada ação chama o endpoint correspondente e atualiza a tabela.

### 3. Modal de Deploy
- Botão "Novo Serviço" no topo da listagem.
- Modal/formulário com campos:
  - Nome do serviço
  - Imagem Docker
  - Porta interna
  - CPU Limit (slider ou input numérico)
  - RAM Limit (slider ou input numérico)
  - Custo por hora (R$)
  - Variáveis de ambiente (key-value dinâmico)
  - Volumes (host path, container path, readonly toggle)
- Submete via `POST /api/containers`.

### 4. Página de Detalhes do Container
- Ao clicar no nome do container na tabela, navegar para página de detalhes.
- Exibe: informações gerais, volumes, env vars, logs recentes, gráfico de métricas.
- Aba de Logs — chama `GET /api/containers/{id}/logs?tail=100`.

## Critérios de Aceite
* Cliente vê apenas seus containers e seu custo.
* Botões de ação alteram o estado real do Docker (confirmado via `docker ps`).
* Busca por texto funciona corretamente.
* Deploy de novo container via modal funciona.
