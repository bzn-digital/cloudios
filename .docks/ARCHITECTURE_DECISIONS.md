# Architecture Decision Records — BZN Cloudios

> Cada ADR documenta uma decisão arquitetural significativa, incluindo o contexto, a decisão tomada e as consequências. Estes registros são imutáveis — uma vez registrados, servem como fonte de verdade para agentes de IA.

---

## ADR-001: Separação API AOT + Blazor WebAssembly

**Status:** Aprovado

**Contexto:** Native AOT não suporta Blazor Server com componentes interativos, que dependem de reflection em runtime para instanciar componentes e renderizar árvores. O projeto exige baixo consumo de RAM no servidor.

**Decisão:** Adotar a Opção B — Blazor WebAssembly. A WebAPI será Native AOT e servirá os arquivos estáticos do Blazor WASM via `UseStaticFiles()`. O processamento da UI roda inteiramente no client-side (browser), economizando 100% da RAM do servidor para a UI.

**Consequências:**
- (+) RAM do servidor dedicada exclusivamente à API e orquestração
- (+) WebAPI compila em Native AOT sem restrições de Blazor
- (+) UI responsiva sem round-trips de renderização server-side
- (-) Necessidade de um projeto Blazor WASM separado no solution
- (-) O browser do cliente precisa baixar o payload WASM inicial (~2-5MB)
- (-) Comunicação UI↔API exclusivamente via HTTP calls (HttpClient no WASM)

**Estrutura de Projetos:**
```
Bzn.Cloudios.sln
├── src/Bzn.Cloudios.Domain/          # Entidades, enums, interfaces
├── src/Bzn.Cloudios.Infrastructure/  # EF Core, Docker, SQLite
├── src/Bzn.Cloudios.Application/     # Serviços de negócio, event bus
├── src/Bzn.Cloudios.WebAPI/          # Minimal APIs + YARP (AOT)
└── src/Bzn.Cloudios.WebApp/          # Blazor WebAssembly (não-AOT)
```

---

## ADR-002: SQLite Split — Dois Bancos de Dados

**Status:** Aprovado

**Contexto:** SQLite WAL permite múltiplos leitores concorrentes mas apenas 1 escritor por vez. O Metrics Worker escreve N linhas a cada 60s enquanto a API também realiza writes esporádicos. Com 50+ containers, o `SQLITE_BUSY` ocorrerá frequentemente se ambos compartilham o mesmo arquivo.

**Decisão:** Separar em dois bancos:
- `cloudios_main.db` — Dados transacionais: Realms, Users, Containers, configurações
- `cloudios_metrics.db` — Série temporal: ContainerMetrics_History

**Consequências:**
- (+) Elimina contenção de escrita entre API e Metrics Worker
- (+) Permite estratégias de backup distintas (main = frequente, metrics = esporádico)
- (+) Métricas podem ser truncadas/deletadas sem afetar dados transacionais
- (-) Dois DbContexts distintos no projeto
- (-) Cross-database queries não são possíveis (mas não são necessárias)
- (-) Migrações devem ser gerenciadas separadamente

**Configuração:**
```
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA busy_timeout=5000;
PRAGMA cache_size=-64000;  -- 64MB cache
```

---

## ADR-003: Event Bus In-Process com System.Threading.Channels

**Status:** Aprovado

**Contexto:** Módulos precisam se comunicar sem acoplamento direto. Quando um container sobe, YARP atualiza rotas e Billing registra início de cobrança. MediatR usa reflection e é incompatível com AOT.

**Decisão:** Implementar `IEventBus` usando `Channel<T>` do `System.Threading.Channels`. Publicação síncrona (fire-and-forget), consumo assíncrono via `BackgroundService` que processa eventos em ordem.

**Consequências:**
- (+) 100% AOT-safe — sem reflection
- (+) Zero dependência externa
- (+) Back-pressure natural via `Channel.Bounded`
- (-) Apenas in-process — não funciona entre instâncias (não é requisito)
- (-) Sem persistência de eventos — se o processo crashar, eventos em trânsito se perdem

**Contrato:** Ver `EVENT_SYSTEM.md` para detalhes completos.

---

## ADR-004: Docker.DotNet com JsonSerializerContext Wrappers

**Status:** Aprovado (com fallback)

**Contexto:** `Docker.DotNet` usa `System.Text.Json` internamente com reflection para desserializar respostas da Docker API. Isso quebra em AOT.

**Decisão:** Tentar `Docker.DotNet` com `JsonSerializerContext` wrappers manuais para os tipos usados. Se o AOT reclamar em tempo de compilação, fazer fallback para `HttpClient` chamando o socket Unix `/var/run/docker.sock` diretamente com serialização via Source Generators.

**Consequências:**
- (+) Se funcionar, API tipada completa do Docker
- (+) Fallback garante que o projeto não fica bloqueado
- (-) Pode exigir wrappers extensos para os tipos do Docker.DotNet
- (-) Fallback com HttpClient requer parsing manual das respostas da Docker Engine API

**Estratégia de fallback:**
```csharp
// Tentativa 1: Docker.DotNet com AOT wrappers
// Se dotnet publish falhar →
// Tentativa 2: HttpClient Unix socket + JsonSerializerContext próprio
```

---

## ADR-005: Extension Methods Explícitos em vez de Global Query Filters

**Status:** Aprovado

**Contexto:** EF Core Global Query Filters com `HasQueryFilter(c => c.RealmId == _tenantProvider.RealmId)` capturam uma referência de serviço resolvida em runtime. Em AOT com compiled models, os filtros precisam ser estáticos ou usar padrões compatíveis.

**Decisão:** Usar extension methods nos repositórios que aplicam `.Where(c => c.RealmId == realmId)` explicitamente em cada query. O `ITenantProvider` fornece o `RealmId` do JWT e o repositório o aplica manualmente.

**Consequências:**
- (+) 100% AOT-safe e explícito
- (+) Fácil de testar — sem mágica de query filter
- (+) Queries são visíveis e auditáveis
- (-) Exige disciplina — cada query DEVE chamar `.ForRealm(realmId)`
- (-) Risco de esquecimento se um desenvolvedor/IA não aplicar o filtro

**Mitigação:** Code review obrigatório verificando que toda query em entidades com `RealmId` usa `.ForRealm()`.

---

## ADR-006: Blazor WebAssembly como Frontend

**Status:** Aprovado

**Contexto:** Com a Opção B (ADR-001), o Blazor WASM roda no browser do cliente. A WebAPI AOT serve os arquivos estáticos do WASM e também atua como backend.

**Decisão:** O projeto `Bzn.Cloudios.WebApp` será um Blazor WebAssembly App (standalone ou hosted). A WebAPI servirá os arquivos de `wwwroot` do WASM via `app.UseStaticFiles()` e `app.MapFallbackToFile("index.html")`.

**Consequências:**
- (+) UI interativa sem consumo de RAM no servidor
- (+) Atualização em tempo real via SSE ou polling leve
- (+) Separação clara entre frontend e backend
- (-) O cliente precisa baixar o runtime WASM na primeira visita
- (-) Todas as chamadas de dados são HTTP requests ao backend
- (-) SEO não é relevante (painel autenticado)

**Padrão de comunicação:**
- WASM → API: `HttpClient` com JWT no header `Authorization: Bearer {token}`
- API → WASM (tempo real): Server-Sent Events (`text/event-stream`) para métricas ao vivo

---

## ADR-007: Namespace Raiz — Bzn.Cloudios

**Status:** Aprovado

**Contexto:** A Task 01 original propunha `PaaS.*` como namespace, que é genérico, conflitante e não reflete a identidade do projeto.

**Decisão:** Namespace raiz oficial: `Bzn.Cloudios.*`. Todos os projetos, pastas e tipos seguem este padrão.

**Consequências:**
- (+) Identidade clara e única no ecossistema .NET
- (+) Sem conflito com pacotes NuGet de terceiros
- (-) Namespace mais longo — aceitável dado o ganho de clareza

**Mapeamento:**
| Projeto | Namespace |
|---------|-----------|
| Domain | `Bzn.Cloudios.Domain` |
| Infrastructure | `Bzn.Cloudios.Infrastructure` |
| Application | `Bzn.Cloudios.Application` |
| WebAPI | `Bzn.Cloudios.WebAPI` |
| WebApp | `Bzn.Cloudios.WebApp` |
