# Naming Conventions — BZN Cloudios

> Este documento define os padrões de nomenclatura obrigatórios para todo o projeto. Desvios destes padrões geram inconsistência que impede agentes de IA de manter coerência.

---

## 1. Namespace Raiz

**`Bzn.Cloudios`** — Sem exceções.

| Projeto | Namespace | Assembly |
|---------|-----------|----------|
| Domain | `Bzn.Cloudios.Domain` | `Bzn.Cloudios.Domain.dll` |
| Infrastructure | `Bzn.Cloudios.Infrastructure` | `Bzn.Cloudios.Infrastructure.dll` |
| Application | `Bzn.Cloudios.Application` | `Bzn.Cloudios.Application.dll` |
| WebAPI | `Bzn.Cloudios.WebAPI` | `Bzn.Cloudios.WebAPI.dll` |
| WebApp | `Bzn.Cloudios.WebApp` | `Bzn.Cloudios.WebApp.dll` |

---

## 2. Projetos e Arquivos

### 2.1 Nomes de Projetos (.csproj)

Formato: `Bzn.Cloudios.{CamelCase}.csproj`

```
src/
├── Bzn.Cloudios.Domain/
│   └── Bzn.Cloudios.Domain.csproj
├── Bzn.Cloudios.Infrastructure/
│   └── Bzn.Cloudios.Infrastructure.csproj
├── Bzn.Cloudios.Application/
│   └── Bzn.Cloudios.Application.csproj
├── Bzn.Cloudios.WebAPI/
│   └── Bzn.Cloudios.WebAPI.csproj
└── Bzn.Cloudios.WebApp/
    └── Bzn.Cloudios.WebApp.csproj
```

### 2.2 Nomes de Arquivos

- **Um tipo por arquivo** — O nome do arquivo DEVE ser igual ao nome do tipo
- Exceção: tipos aninhados privados podem estar no arquivo do tipo pai

| Tipo | Arquivo |
|------|---------|
| `ContainerService` | `ContainerService.cs` |
| `IContainerService` | `IContainerService.cs` |
| `ContainerDeployRequest` | `ContainerDeployRequest.cs` |
| `ContainerJsonContext` | `ContainerJsonContext.cs` |

---

## 3. Tipos (Classes, Interfaces, Enums, Records)

### 3.1 Classes

- **PascalCase**: `ContainerService`, `RealmRepository`
- **Sufixo por categoria:**

| Categoria | Sufixo | Exemplo |
|-----------|--------|---------|
| Serviço de aplicação | `Service` | `ContainerService` |
| Repositório | `Repository` | `ContainerRepository` |
| Background worker | `Worker` | `MetricsCollectionWorker` |
| Interceptor | `Interceptor` | `SqlitePragmaInterceptor` |
| Exception customizada | `Exception` | `ContainerNotFoundException` |
| DTO Request | `Request` | `ContainerDeployRequest` |
| DTO Response | `Response` | `ContainerResponse` |
| DTO Paged | `PagedResponse<T>` | `PagedResponse<ContainerResponse>` |
| Evento | `Event` | `ContainerStartedEvent` |
| Resultado | `Result` | `OperationResult` |
| JsonSerializerContext | `JsonContext` | `ContainerJsonContext` |
| Endpoint static class | `Endpoints` | `ContainerEndpoints` |

### 3.2 Interfaces

- Prefixo `I` + PascalCase: `IContainerService`, `IEventBus`, `ITenantProvider`

### 3.3 Enums

- **PascalCase** para o nome do enum e para cada membro
- Sem prefixo `e` ou `Enum`

```csharp
public enum ContainerStatus
{
    Running,
    Stopped,
    Failed,
    Deploying
}
```

### 3.4 Entidades do Domain

- **PascalCase**, sem sufixo
- Propriedades: **PascalCase**

```csharp
public sealed class Container
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ContainerStatus Status { get; set; }
}
```

---

## 4. Membros (Propriedades, Métodos, Campos)

### 4.1 Propriedades

- **PascalCase**: `RealmId`, `CostPerHourBRL`, `StartedAtUtc`

### 4.2 Métodos

- **PascalCase**: `GetByIdAsync`, `DeployAsync`, `ForRealm`
- Métodos async DEVEM ter sufixo `Async`: `GetAllAsync`, `StartContainerAsync`
- Extension methods para queries: verbo + contexto: `ForRealm`, `WithStatus`, `OrderedByCreated`

### 4.3 Campos Privados

- **camelCase** com underscore prefix: `_logger`, `_dbContext`, `_tenantProvider`

```csharp
public sealed class ContainerService
{
    private readonly IContainerRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ContainerService> _logger;
}
```

### 4.4 Constantes

- **PascalCase** para constantes públicas
- **camelCase** com underscore prefix para constantes privadas

```csharp
public const string DockerLabelRealm = "cloudios.realm";
private const int _metricsIntervalSeconds = 60;
```

---

## 5. Variáveis Locais e Parâmetros

- **camelCase**: `realmId`, `containerName`, `cancellationToken`

---

## 6. URLs e Rotas de API

- **kebab-case**: `/api/containers`, `/api/container-metrics`, `/api/auth/login`
- IDs em rotas: `/{id:guid}`
- Actions em rotas: `/{id:guid}/start`, `/{id:guid}/stop`

---

## 7. Labels Docker

- Prefixo `cloudios.`: `cloudios.realm`, `cloudios.container`, `cloudios.managed`

---

## 8. Variáveis de Ambiente

- **SCREAMING_SNAKE_CASE**: `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `CLOUDIOS_DATA_DIR`

---

## 9. Banco de Dados

### 9.1 Tabelas

- **PascalCase plural**: `Realms`, `Users`, `Containers`, `ContainerMetrics_History`

### 9.2 Colunas

- **PascalCase**: `RealmId`, `CostPerHourBRL`, `StartedAtUtc`

### 9.3 Índices

- Prefixo `IX_` + Nome da Tabela + Colunas: `IX_Containers_RealmId_Status`

---

## 10. Arquivos de Configuração

| Arquivo | Formato |
|---------|---------|
| `appsettings.json` | camelCase para chaves |
| `appsettings.Production.json` | camelCase para chaves |
| `.github/workflows/*.yml` | kebab-case para nomes de arquivos |

---

## 11. Git

### 11.1 Branches

- `feature/add-container-volumes`
- `fix/metrics-batch-insert`
- `hotfix/jwt-expiry-bug`
- `release/v1.0.0`

### 11.2 Commits

Formato: `type(scope): description`

Tipos: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `ci`

Exemplos:
```
feat(containers): add volume mapping support
fix(billing): correct hourly rate calculation
docs(api): update container endpoints contract
```
