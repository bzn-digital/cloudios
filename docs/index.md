---
title: BZN Cloudios
layout: home
---

# 🌩️ BZN Cloudios

**The self-hosted native cloud engine.**

O **BZN Cloudios** é uma Plataforma como Serviço (PaaS) privada, ultraleve e de alto desempenho, desenvolvida pela [BZN Digital](https://github.com/bzn-digital). Projetada para orquestrar containers Docker diretamente no host Linux, ela atua como um sistema operacional em nuvem, permitindo o deploy "Plug n Play" de aplicações com isolamento total de recursos.

## 🚀 Visão Geral da Arquitetura

O Cloudios foi construído com foco extremo em **baixo consumo de recursos e alta performance**.

- **Core:** .NET 10 com compilação **Native AOT** (Ahead-of-Time)
- **Frontend:** Blazor WebApp para painéis administrativos e React para painel de clientes
- **Orquestração:** Comunicação nativa com o daemon do Docker/Podman Linux
- **Rede:** YARP (Yet Another Reverse Proxy) rodando em memória com suporte a rotas dinâmicas
- **Banco de Dados:** SQLite (WAL Mode) otimizado para concorrência e baixo I/O
- **Segurança e Exposição:** Integração nativa para rodar atrás de Cloudflare Tunnels (Zero Trust)

## 🏢 Sistema de Multi-Tenancy (Realms)

O Cloudios não é apenas um orquestrador, é uma plataforma de hospedagem comercializável.

- **Isolamento Lógico:** Os clientes são divididos em `Realms`. Nenhum dado ou métrica vaza entre clientes
- **Controle de Hardware:** Cada serviço possui limites estritos de CPU e Memória RAM gerenciados pela plataforma
- **Motor de Bilhetagem:** Coleta em tempo real do histórico de consumo para faturamento financeiro (BRL) individualizado por serviço

## 📋 Documentação

- [Managed Apps](managed-apps.md) - Documentação completa de aplicações gerenciadas
- [Git Workflow](https://github.com/bzn-digital/cloudios/blob/main/RULES.md) - Regras de governança e workflow
- [Changelog](https://github.com/bzn-digital/cloudios/blob/main/CHANGELOG.md) - Histórico de mudanças

## ⚡ Funcionalidades

### Managed Apps
- Deploy automático de containers Docker/Podman
- Isolamento de recursos por instância
- Monitoramento em tempo real
- Bilhetagem por hora baseada em consumo

### Managed Databases
- Deploy de bancos de dados gerenciados
- Tiers configuráveis (Nano, Micro, Small, Medium, Large)
- Backup automático
- Escalabilidade vertical

### Multi-Tenancy
- Isolamento completo por realm
- Redes Docker isoladas
- Autenticação JWT por realm
- Controle de acesso por roles

## 🛠️ Desenvolvimento

### Pré-requisitos
- .NET 10 SDK
- Node.js 20+
- Docker ou Podman
- SQLite

### Executar Localmente

```bash
# Iniciar Podman socket (se usando Podman)
systemctl --user start podman.socket

# Executar WebAPI
cd src/Bzn.Cloudios.WebAPI
dotnet run

# Executar WebApp
cd src/Bzn.Cloudios.WebApp
npm run dev

# Executar WebPlatform
cd src/Bzn.Cloudios.WebPlatform
npm run dev
```

### Endpoints
- WebAPI: `http://localhost:5021`
- WebApp: `http://localhost:5173`
- WebPlatform: `http://localhost:5174`

## 📦 Git Workflow

Este repositório segue um modelo de versionamento estruturado:

- `main`: Código em produção. Protegida. Aceita apenas merges de branches de release e hotfixes
- `development`: Ambiente de integração contínua
- Padrões de branch: `feature/*`, `bugfix/*`, `hotfix/*`, `release/v*`

Veja [RULES.md](https://github.com/bzn-digital/cloudios/blob/main/RULES.md) para detalhes completos.

## ⚖️ Licença

Este projeto é regido pela **GNU Affero General Public License v3.0 (AGPLv3)**.

O código-fonte está disponível sob os termos da AGPLv3, que exige que quaisquer modificações distribuídas (incluindo uso via rede) sejam disponibilizadas sob a mesma licença.

---

*Built with ⚡ by [BZN Digital](https://github.com/bzn-digital)*
