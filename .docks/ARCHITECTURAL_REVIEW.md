# Relatório de Revisão Arquitetural — BZN Cloudios

> Gerado em: 01/06/2026
> Escopo: Revisão crítica das tasks, gaps arquiteturais e documentação necessária para desenvolvimento autônomo por IA.

---

## 1. Revisão das Tasks

### 1.1 Contradição Imediata: Licença

O `README.md` declara **Business Source License 1.1 (BSL)** na linha 29, mas a Task 00 exige **AGPLv3**. Isso é um bloqueio para qualquer agente de IA — qual prevalece? A decisão precisa ser unificada **antes** de qualquer código.

### 1.2 Contradição Estrutural: Namespace vs. Nome do Projeto

A Task 01 define os projetos como `PaaS.Domain`, `PaaS.Infrastructure`, etc. O projeto se chama **BZN Cloudios**. O namespace raiz deveria ser `Cloudios.*` ou `Bzn.Cloudios.*` — nunca `PaaS.*`, que é genérico e conflitante.

### 1.3 Análise Cronológica e de Tamanho

| Task | Tamanho | Risco | Veredito |
|------|---------|-------|----------|
| 00 - Governança | OK | Baixo | ✅ Mantém |
| 01 - Setup .NET AOT | **Muito fina** | Médio | ⚠️ Precisa de substância — não define versão do SDK, não lista pacotes NuGet, não especifica estrutura de pastas |
| 02 - Database SQLite | **Muito fina** | Alto | ⚠️ Não define índices compostos, não aborda migrações em AOT, não define estratégia de seeding |
| 03 - Auth e Tenancy | OK | Médio | ✅ Mas precisa desmembrar (ver abaixo) |
| 04 - Docker Lifecycle | **Grande demais** | Alto | ❌ Desmembrar em 3 subtasks |
| 05 - YARP/Cloudflare | OK | Médio | ✅ Mas muito superficial |
| 06 - Metrics Engine | OK | Baixo | ✅ Mantém |
| 07 - Billing Engine | OK | Baixo | ✅ Mantém |
| 08 - Client Panel | **Grande demais** | Alto | ❌ Desmembrar em 3 subtasks |
| 09 - Admin Panel | **Grande demais** | Alto | ❌ Desmembrar em 2 subtasks |

### 1.4 Desmembramentos Sugeridos

**Task 04 → 3 tasks:**

- **04a — Docker Client Service:** Integração `Docker.DotNet`, operações CRUD de containers, labels, limites de recurso
- **04b — Container API Endpoints:** Minimal APIs `/api/containers/*` com validação, DTOs e autorização
- **04c — Container Event System:** Eventos de ciclo de vida (`ContainerStarted`, `ContainerStopped`) para que YARP e Billing reajam — **isto não existe em nenhuma task e é um gap crítico**

**Task 08 → 3 tasks:**

- **08a — Client Layout e Auth State:** Layout Blazor com sidebar, `AuthStateProvider`, redirecionamento por role
- **08b — Client Dashboard:** Cards de custo + gráficos de métricas
- **08c — Client Service Manager:** Tabela de containers com busca, botões de ação

**Task 09 → 2 tasks:**

- **09a — Admin Dashboard Global:** Cards de faturamento total, consumo do host
- **09b — Admin Realm & Container Manager:** Tabelas de realms e containers globais com ações de emergência

### 1.5 Tasks Ausentes (Gaps Funcionais)

Nenhuma task cobre estes requisitos essenciais para uma PaaS funcional:

- **Realm CRUD:** Como criar, editar e desativar um Realm? (A Task 09 menciona "Bloquear Acesso" mas não há API de criação)
- **User Management:** Como criar usuários dentro de um Realm? Não há endpoint de registro ou gerenciamento de usuários
- **Container Volumes / Persistent Storage:** Nenhuma menção a volumes Docker — sem isso, dados do cliente se perdem no restart
- **Container Environment Variables:** Deploy sem env vars é inútil para qualquer aplicação real
- **Container Port Mapping:** A Task 05 assume que containers têm IP interno, mas não define como a porta do container é especificada no deploy
- **Container Logs:** Nenhuma task prevê visualização de logs do container — essencial para debug
- **Dockerfile da PaaS:** Como o próprio Cloudios roda em Docker? Não há task de containerização da aplicação
- **Health Checks:** Nenhum endpoint de health para o Cloudflare Tunnel verificar

---

## 2. Gaps Arquiteturais Críticos

### 2.1 🔴 BLOQUEADOR: Blazor + Native AOT são incompatíveis

**Este é o gap mais grave do projeto.** Blazor Server / Blazor WebApp com componentes interativos (`@rendermode InteractiveServer`) depende de **reflection em runtime** para instanciar componentes, resolver parâmetros e renderizar árvores. Native AOT **não suporta isso**.

**Soluções possíveis:**

| Opção | Viabilidade | Trade-off |
|-------|-------------|-----------|
| **A. Blazor SSR-only** (sem interatividade server) + Enhanced Forms | ✅ Funciona com AOT | UX limitada — sem atualização em tempo real, tudo é form post + full page navigation |
| **B. Blazor WebAssembly para interatividade** | ✅ Funciona | WASM roda no browser (não no server AOT). Precisa de um projeto separado e API consumption |
| **C. Separar em 2 projetos:** API (AOT) + Blazor UI (JIT, não-AOT) | ✅ Mais pragmático | Perde o "mesmo projeto" mas mantém Blazor interativo. API continua AOT |
| **D. Substituir Blazor por SPA externo** (React/Vue consumindo a API) | ✅ Máxima flexibilidade | Sai do ecossistema .NET no frontend |

**Recomendação:** Opção **C** — dois projetos no mesmo solution. `Cloudios.WebAPI` (AOT, Minimal APIs + YARP) e `Cloudios.WebApp` (Blazor Server, não-AOT, consumindo as APIs internamente). O binário AOT é o que vai para produção com ~30MB; o Blazor roda como processo separado ou embedded.

### 2.2 🟡 SQLite: Contenção de Escrita e Volume de Métricas

SQLite WAL permite **muitos leitores concorrentes, mas apenas 1 escritor por vez**. O cenário do Cloudios:

- Metrics Worker: escreve N linhas a cada 60s (1 por container ativo)
- API: escreve status de container em operações de start/stop
- Billing: lê (não escreve frequentemente)

Com 50 containers: ~50 writes/min no metrics + writes esporádicos da API. O `SQLITE_BUSY` vai ocorrer. Solução:

- **Batch insert** no Metrics Worker: acumular todas as métricas do ciclo e inserir em uma única transação
- **Separar bancos:** `cloudios_main.db` (dados transacionais: realms, users, containers) e `cloudios_metrics.db` (série temporal). Isso elimina contenção entre API e Metrics
- **Definir `PRAGMA busy_timeout=5000;`** para retry automático em vez de falhar imediatamente

### 2.3 🟡 Docker.DotNet + AOT

`Docker.DotNet` usa `System.Text.Json` internamente com reflection para desserializar respostas da API do Docker. **Não tem Source Generators nativos.** Isso vai quebrar em AOT.

**Soluções:**

- Usar `Docker.DotNet` apenas no projeto não-AOT (se seguir a Opção C acima, o service fica no Infrastructure que é compartilhado — precisa de atenção)
- **Alternativa AOT-safe:** Invocar `docker` CLI via `System.Diagnostics.Process` e parsear output. Menos elegante, mas 100% AOT-safe e sem dependência de biblioteca com reflection
- Se usar `Docker.DotNet`, criar `JsonSerializerContext` wrappers manuais para os tipos usados e registrar via `DockerClientConfiguration`

### 2.4 🟡 EF Core Global Query Filter + AOT

A Task 03 propõe `HasQueryFilter(c => c.RealmId == _tenantProvider.RealmId)`. O filtro captura uma referência de serviço resolvida em runtime. Em AOT, o EF Core precisa de **compiled models** (`UseModel`) e os filtros precisam ser estáticos ou usar padrões AOT-compatíveis.

**Solução:** Em vez de Global Query Filter (que exige expressão lambda capturando variável), usar **extension method nos repositórios** que aplica `.Where(c => c.RealmId == tenantProvider.RealmId)` explicitamente em cada query. Menos "mágico" mas 100% AOT-safe e explícito.

### 2.5 🟡 YARP Rotas Dinâmicas + AOT

YARP em .NET 9+ tem suporte AOT, mas a atualização dinâmica de rotas em memória (injetar/remover destinos em runtime) precisa ser feita via `IProxyStateLookup` ou manipulação programática do `IReadOnlyDictionary` de rotas — não via recarregamento de config JSON (que usa reflection). A Task 05 não especifica o mecanismo.

### 2.6 🟡 Ausência de Event Bus / Pub-Sub Interno

Não há nenhum mecanismo de comunicação entre os módulos. Quando um container sobe (Task 04), o YARP precisa atualizar rotas (Task 05) e o Billing precisa registrar o início de cobrança (Task 07). Sem um event system, os serviços ficarão acoplados por chamadas diretas ou polling.

**Solução:** Implementar um `IEventBus` simples (in-process, usando `Channel<T>` do `System.Threading.Channels`) com eventos como `ContainerStartedEvent`, `ContainerStoppedEvent`. Cada módulo publica/assina. AOT-safe, zero reflection, zero dependência externa.

---

## 3. Documentação Mestra Necessária

Para que agentes de IA desenvolvam com zero alucinação, os seguintes documentos são **obrigatórios** antes de qualquer linha de código:

### 3.1 Por que cada documento é necessário

| Documento | Evita qual alucinação |
|-----------|----------------------|
| `AI_CODING_GUIDELINES.md` | IA gerando código com reflection, dynamic, ou padrões não-AOT |
| `ARCHITECTURE_DECISIONS.md` | IA revertendo decisões já tomadas (ex: tentar usar Blazor interativo no projeto AOT) |
| `DATABASE_SCHEMA.md` | IA criando tabelas sem índices, com tipos errados, ou sem respeitar o modelo relacional |
| `API_CONTRACT.md` | IA inventando endpoints, DTOs inconsistentes, ou HTTP methods errados |
| `NAMING_CONVENTIONS.md` | IA misturando `PaaS.*` com `Cloudios.*`, ou usando PascalCase em URLs |
| `EVENT_SYSTEM.md` | IA criando acoplamento direto entre serviços em vez de usar o event bus |
| `DEPLOYMENT_TOPOLOGY.md` | IA assumindo que o Cloudios roda "na nuvem" quando é self-hosted com Docker socket |

---

## 4. Proposta de Estrutura de Documentação

```
.docks/
├── tasks/                              # (existente — tasks de implementação)
│   ├── 00_Repo_Governance_And_Workflow.md
│   ├── 01_DotNet10_AOT_Setup.md
│   ├── ... (tasks existentes + novas)
│   ├── 10_Realm_And_User_Management.md        # NOVA
│   ├── 11_Container_Volumes_And_EnvVars.md    # NOVA
│   ├── 12_Container_Logs_Viewer.md            # NOVA
│   ├── 13_PaaS_Dockerfile_And_Deploy.md       # NOVA
│   └── 14_Health_Checks_And_Monitoring.md     # NOVA
│
├── AI_CODING_GUIDELINES.md             # NOVO — regras rígidas para IA
├── ARCHITECTURE_DECISIONS.md           # NOVO — ADRs (Architecture Decision Records)
├── DATABASE_SCHEMA.md                  # NOVO — schema completo com índices e relações
├── API_CONTRACT.md                     # NOVO — todos os endpoints, DTOs e status codes
├── NAMING_CONVENTIONS.md              # NOVO — padrões de nomenclatura
├── EVENT_SYSTEM.md                     # NOVO — contratos de eventos internos
└── DEPLOYMENT_TOPOLOGY.md              # NOVO — diagrama de infra e fluxo de rede
```

### Conteúdo de cada documento:

#### `AI_CODING_GUIDELINES.md` — O documento mais crítico

- Proibição absoluta de `reflection`, `dynamic`, `Activator.CreateInstance`, e qualquer API que dependa de runtime type discovery
- Padrão obrigatório de `JsonSerializerContext` para todos os DTOs com exemplo concreto
- Padrão de registro manual de DI (sem `Assembly.Scan`, sem `AddAutoMapper`, sem decorators dinâmicos)
- Padrão de nulo: usar `??` e `?.` explicitamente, nunca `!` (null-forgiving) sem guarda
- Proibição de `Lazy<T>` com construtor sem parâmetros (usa reflection internamente)
- Lista de pacotes NuGet **aprovados** (AOT-safe) e **proibidos** (não-AOT-safe)
- Template de DTO com `[JsonSerializable]` e `[JsonDerivedType]` para polimorfismo

#### `ARCHITECTURE_DECISIONS.md` — ADRs numerados

- ADR-001: Separação API AOT + Blazor não-AOT (Opção C)
- ADR-002: SQLite split em 2 bancos (transacional + métricas)
- ADR-003: Event bus in-process via `Channel<T>` em vez de MediatR (não-AOT-safe)
- ADR-004: Docker.DotNet com wrappers AOT vs. CLI direto
- ADR-005: Repositórios explícitos em vez de Global Query Filters
- ADR-006: Blazor SSR + Enhanced Forms como padrão de UI (se seguir Opção A)

#### `DATABASE_SCHEMA.md` — Schema completo

- DDL SQL completo com tipos, constraints, índices e FKs
- Índices compostos ausentes nas tasks: `IX_Containers_RealmId_Status`, `IX_Metrics_ContainerId_Timestamp`
- Estratégia de migrações compatível com AOT (EF Core compiled models)
- Estratégia de seeding (admin padrão, realm padrão)

#### `API_CONTRACT.md` — Contrato completo

- Tabela de todos os endpoints: Method, Path, Request DTO, Response DTO, Auth Required, Role Required
- Exemplo de payload e response para cada endpoint
- Códigos de erro padronizados (400, 401, 403, 404, 409, 500)
- Padrão de paginação (offset/limit ou cursor)

#### `NAMING_CONVENTIONS.md` — Padrões

- Namespace raiz: `Cloudios.*` (não `PaaS.*`)
- Projetos: `Cloudios.Domain`, `Cloudios.Infrastructure`, `Cloudios.Application`, `Cloudios.WebAPI`, `Cloudios.WebApp`
- DTOs: `{Entity}Request` / `{Entity}Response` / `{Entity}Event`
- Endpoints: kebab-case em URLs (`/api/container-metrics`)
- Arquivos: um tipo por arquivo, nome do arquivo = nome do tipo

#### `EVENT_SYSTEM.md` — Contratos de eventos

- Definição da interface `IEventBus` e implementação com `Channel<T>`
- Lista de todos os eventos: `ContainerStartedEvent`, `ContainerStoppedEvent`, `ContainerDeletedEvent`, `ContainerFailedEvent`
- Payload de cada evento (DTO AOT-safe)
- Quem publica e quem assina cada evento

#### `DEPLOYMENT_TOPOLOGY.md` — Infraestrutura

- Diagrama textual: Cloudflare → cloudflared → YARP → Containers Docker
- Fluxo de rede com portas internas
- Como o Cloudios roda (Docker-in-Docker? Socket mount?)
- Variáveis de ambiente necessárias
- Configuração do cloudflared

---

## Resumo Executivo

- **1 bloqueador crítico:** Blazor interativo é incompatível com Native AOT. Decidir a estratégia (recomendo Opção C: 2 projetos) antes de prosseguir.
- **5 gaps arquiteturais** não previstos nas tasks (contention SQLite, Docker.DotNet AOT, Query Filters AOT, YARP dinâmico, ausência de Event Bus).
- **6 tasks ausentes** que uma PaaS funcional precisa (Realm CRUD, User Management, Volumes, Env Vars, Logs, Dockerfile).
- **3 tasks grandes demais** que devem ser desmembradas (04, 08, 09).
- **7 documentos de referência** devem ser criados antes de codar, sendo `AI_CODING_GUIDELINES.md` e `ARCHITECTURE_DECISIONS.md` os mais urgentes.
- **2 contradições** a resolver: Licença (BSL vs AGPLv3) e Namespace (`PaaS.*` vs `Cloudios.*`).
