# 🌩️ BZN Cloudios
**The self-hosted native cloud engine.**

O **BZN Cloudios** é uma Plataforma como Serviço (PaaS) privada, ultraleve e de alto desempenho, desenvolvida pela [BZN Digital](https://suaempresa.com). Projetada para orquestrar containers Docker diretamente no host Linux, ela atua como um sistema operacional em nuvem, permitindo o deploy "Plug n Play" de aplicações com isolamento total de recursos.

## 🚀 Visão Geral da Arquitetura
O Cloudios foi construído com foco extremo em **baixo consumo de recursos e alta performance**.

* **Core:** .NET 10 com compilação **Native AOT** (Ahead-of-Time).
* **Frontend:** Blazor WebApp para painéis administrativos e de clientes.
* **Orquestração:** Comunicação nativa com o daemon do Docker Linux.
* **Rede:** YARP (Yet Another Reverse Proxy) rodando em memória com suporte a rotas dinâmicas.
* **Banco de Dados:** SQLite (WAL Mode) otimizado para concorrência e baixo I/O.
* **Segurança e Exposição:** Integração nativa para rodar atrás de Cloudflare Tunnels (Zero Trust).

## 🏢 Sistema de Multi-Tenancy (Realms)
O Cloudios não é apenas um orquestrador, é uma plataforma de hospedagem comercializável.
* **Isolamento Lógico:** Os clientes são divididos em `Realms`. Nenhum dado ou métrica vaza entre clientes.
* **Controle de Hardware:** Cada serviço possui limites estritos de CPU e Memória RAM gerenciados pela plataforma.
* **Motor de Bilhetagem:** Coleta em tempo real do histórico de consumo para faturamento financeiro (BRL) individualizado por serviço.

## 📋 Git Workflow
Este repositório segue um modelo de versionamento estruturado:
* `main`: Código em produção. Protegida. Aceita apenas merges de branches de release e hotfixes.
* `development`: Ambiente de integração contínua.
* Padrões de branch: `feature/*`, `bugfix/*`, `hotfix/*`, `release/v*`.

## ⚖️ Licença
Este projeto é regido pela **Business Source License 1.1 (BSL)** (ou especifique a PolyForm aqui). 
O código-fonte está disponível para leitura, estudo e uso pessoal/acadêmico. **É estritamente proibido o uso comercial desta infraestrutura para prover serviços concorrentes de hospedagem/PaaS sem autorização expressa da BZN Digital.**

---
*Built with ⚡ by BZN Digital.*
