# Task 17: Blazor WASM (`WebPlatform`) — Painel Administrativo Global (Dashboard)

## Objetivo
Construir o Dashboard principal do portal administrativo (`Bzn.Cloudios.WebPlatform`). Esta tela é a central de comando exclusiva para usuários com a role `GlobalAdmin`, fornecendo visão macro do faturamento total da PaaS e da saúde do servidor físico (Host).

## Requisitos de Implementação

### 1. Dashboard Global (Cards de Visão Geral)
- **Receita Mensal Total:** Chama `GET /api/billing/global`. Exibe a soma do faturamento de todos os Realms no mês corrente em Reais (R$), com indicativo visual de faturamento.
- **Saúde do Servidor (Host RAM/CPU):** Chama `GET /api/metrics/host`. Diferente das métricas de container, este endpoint deve retornar o uso total da máquina física onde o Docker está rodando. Exibir através de barras de progresso circulares ou lineares (ex: "RAM do Servidor: 64% em uso").
- **Containers Ativos Globais:** Contagem total de instâncias rodando atualmente no servidor.
- **Realms Ativos:** Contagem total de clientes ativos na plataforma.

### 2. Seção de Gráficos Analíticos
- **Gráfico de Faturamento por Cliente (Realm):** Gráfico de barras (horizontais ou verticais) mostrando o *Top 10 Realms* que mais geraram custos no mês.
- **Gráfico de Estresse do Servidor:** Gráfico de linha temporal cruzando a CPU e a RAM total do Host nas últimas 24 horas (se os dados de histórico do host estiverem sendo armazenados na Task 06).
- *Nota Técnica:* Utilizar a mesma biblioteca gráfica leve escolhida para o `WebApp` (ex: JSInterop com Chart.js ou ApexCharts) para evitar o download de pacotes WASM pesados e desnecessários.

### 3. Tabela de Receita por Realm
- Uma tabela logo abaixo dos gráficos listando o consolidado de todos os clientes.
- **Colunas Visíveis:** - Nome do Realm.
  - Status (Ativo/Bloqueado).
  - Quantidade de Containers (Rodando / Total).
  - Faturamento Atual (R$).
- **Ordenação:** Por padrão, deve vir ordenada do maior faturamento para o menor.
- **Ação:** Clicar em uma linha da tabela deve redirecionar o admin para a página de Detalhes do Realm (que será desenvolvida na Task 18).

## Critérios de Aceite
* A página deve bloquear ativamente a renderização caso o JWT local não possua a role `GlobalAdmin`.
* O cálculo de Faturamento Geral bate exatamente com a soma matemática do faturamento individual de todos os Realms da tabela.
* O componente visual de consumo de CPU/RAM do Host reflete a saúde real da máquina física, permitindo ao administrador prever se precisa de um upgrade no servidor.