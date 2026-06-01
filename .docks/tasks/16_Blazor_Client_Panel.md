# Task 16: Blazor WASM (`WebApp`) — Painel do Cliente (Dashboard e Serviços)

## Objetivo
Construir as páginas operacionais do portal do cliente (`Bzn.Cloudios.WebApp`): o Dashboard (focado em saúde e custos do Realm) e o Gerenciador de Serviços (operações reais de containers Docker).

## Requisitos de Implementação

### 1. Dashboard do Cliente (Realm Overview)
- **Card: Gasto Total do Mês:** Chama `GET /api/billing/realm`, exibindo o valor consolidado em R$ com destaque visual.
- **Gráfico de Consumo (CPU/RAM):** Chama `GET /api/metrics/realm/history`. Utilizar uma biblioteca leve compatível com WASM (como `MudBlazor` charts, `ApexCharts.Blazor` ou JSInterop com `Chart.js`) para plotar a linha do tempo de consumo do Realm.
- **Cards de Status:**
  - Contagem de containers com status `Running` (Verde).
  - Contagem de containers com status `Stopped` ou `Failed` (Amarelo/Vermelho).

### 2. Gerenciador de Serviços (Página de Listagem)
- **Tabela de Containers:** Chama `GET /api/containers` passando o token do cliente.
- **Colunas Visíveis:** Status (Badge colorido), Nome do App, Imagem Docker, URL Pública (se configurada via YARP), Custo Acumulado (R$), Limite CPU, Limite RAM.
- **Filtros e Buscas:**
  - Campo de Busca no topo filtrando pelo nome do serviço (query param `search`).
  - Dropdown de Filtro de Status: `All`, `Running`, `Stopped`, `Failed`.
- **Ações por linha (Botões de Controle):**
  - `Start`: Visível apenas se `Stopped/Failed`.
  - `Restart`: Visível se `Running` ou `Failed`.
  - `Stop`: Visível apenas se `Running`.
  - `Delete`: Sempre visível (exige Modal de Confirmação digitando o nome do serviço para evitar exclusão acidental).
  - *Comportamento:* Cada clique deve chamar a Minimal API (ex: `POST /api/containers/{id}/stop`), exibir um *Loading/Toast* de processamento e atualizar a linha da tabela na sequência.

### 3. Modal de Deploy (Novo Serviço)
- Botão primário "New Service" no topo da listagem.
- **Formulário Plug n Play:**
  - `Nome do serviço` (Apenas letras minúsculas e hífens, para virar subdomínio).
  - `Imagem Docker` (ex: `nginx:latest`, `n8nio/n8n`).
  - `Porta Interna do Container` (Porta que o app escuta nativamente, para o YARP saber para onde enviar o tráfego do Cloudflare Tunnel. Ex: `80` ou `5678`).
  - `Limites de Hardware`: CPU Limit (ex: 0.5 vCPU) e RAM Limit (ex: 256MB).
  - `Variáveis de Ambiente`: Lista dinâmica (chave-valor) onde o cliente pode adicionar/remover linhas livremente.
  - *Nota de Segurança:* O "Custo por hora" **não** deve ser um input do cliente. Ele deve ser calculado no backend baseado no plano do Realm ou nos recursos selecionados.
- **Submissão:** Envia o payload via `POST /api/containers`.

### 4. Página de Detalhes do Container (`/services/{id}`)
- Ao clicar no nome do container na tabela, navegar para a visão detalhada.
- **Aba Overview:** Informações gerais, consumo de custo isolado, botão de copiar a URL gerada pelo YARP.
- **Aba Environment:** Visualização (read-only ou editável caso o container esteja parado) das variáveis de ambiente aplicadas.
- **Aba Live Logs:** Um terminal emulando o console. Chama `GET /api/containers/{id}/logs?tail=100` exibindo as últimas saídas do container (se possível, com um botão de "Auto-Refresh" de 5 em 5 segundos).

## Critérios de Aceite
* O cliente enxerga estritamente os containers criados dentro do seu `RealmId`.
* O Modal de Deploy envia corretamente os dados e a API sobe o container no host Docker (verificável via `docker ps`).
* As variáveis de ambiente informadas no UI são injetadas com sucesso no container final.
* Ações manuais de Stop/Start/Delete refletem no host e o estado do botão (Disabled/Loading) impede cliques duplos.