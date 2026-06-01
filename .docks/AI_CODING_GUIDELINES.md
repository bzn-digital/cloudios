# AI Coding Guidelines — BZN Cloudios

> Este documento é a **fonte de verdade absoluta** para qualquer agente de IA ou desenvolvedor que escreva código neste repositório. Violações a estas regras geram código que não compila em Native AOT ou que introduz comportamento indefinido em runtime.

---

## 1. Native AOT — Restrições Absolutas

### 1.1 Proibições (NUNCA use)

| Construção | Motivo | Alternativa |
|-----------|--------|-------------|
| `System.Reflection.*` | Não existe em AOT | Source Generators, código gerado em compile-time |
| `dynamic` | Requer reflection | Tipos concretos, generics com constraints |
| `Activator.CreateInstance()` | Requer reflection | `new()`, factory pattern manual |
| `Assembly.Load()` / `Assembly.GetTypes()` | Proibido em AOT | Registro manual no `Program.cs` |
| `Lazy<T>` com lambda | Usa reflection internamente | `Lazy<T>(T value)` pré-computado ou inicialização eager |
| `System.Text.Json` sem `JsonSerializerContext` | Usa reflection por padrão | Source Generators (ver seção 2) |
| `AddAutoMapper()` | Escaneia assemblies | Mapeamento manual com métodos `ToResponse()` |
| `AddMvcCore().AddApplicationPart()` | Scan de assemblies | Minimal APIs com `AddEndpoints()` |
| `MediatR` | Usa reflection para resolver handlers | `IEventBus` com `Channel<T>` (ver `EVENT_SYSTEM.md`) |
| `EF Core Lazy Loading` | Usa reflection/proxies | Eager loading com `.Include()` |
| `EF Core Global Query Filters` com variável capturada | Incompatível com compiled models AOT | Extension methods explícitos nos repositórios |

### 1.2 Padrão de Registro de DI

**TODOS** os serviços devem ser registrados manualmente no `Program.cs`. Nenhum scan automático.

```csharp
// ✅ CORRETO — Registro manual e explícito
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddScoped<IRealmService, RealmService>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();

// ❌ ERRADO — Qualquer forma de scan dinâmico
builder.Services.Scan(...);
builder.Services.AddAutoMapper(typeof(Startup));
```

### 1.3 Compilação AOT Obrigatória

Todo `.csproj` da solução que será publicado DEVE conter:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

O projeto `Bzn.Cloudios.WebAPI` é o único que publica como AOT. O projeto `Bzn.Cloudios.WebApp` (Blazor WASM) publica como WASM — não usa AOT no server.

O comando de validação é:

```bash
dotnet publish src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj -c Release -r linux-x64
```

Se este comando falhar, o código está quebrado para AOT.

---

## 2. System.Text.Json — Source Generators Obrigatórios

### 2.1 Padrão de JsonSerializerContext

**TODO** DTO que for serializado/desserializado DEVE ter um `JsonSerializerContext` associado.

```csharp
// ✅ CORRETO
[JsonSerializable(typeof(ContainerResponse))]
[JsonSerializable(typeof(ContainerDeployRequest))]
[JsonSerializable(typeof(List<ContainerResponse>))]
public partial class ContainerJsonContext : JsonSerializerContext;
```

Uso:

```csharp
// ✅ CORRETO — Usa o context gerado em compile-time
var json = JsonSerializer.Serialize(container, ContainerJsonContext.Default.ContainerResponse);

// ❌ ERRADO — Usa reflection em runtime
var json = JsonSerializer.Serialize(container);
```

### 2.2 Registro no DI

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.JsonSerializerOptions.TypeInfoResolverChain.Add(ContainerJsonContext.Default);
    options.JsonSerializerOptions.TypeInfoResolverChain.Add(AuthJsonContext.Default);
    // ... adicionar todos os contexts
});
```

### 2.3 Polimorfismo AOT-safe

Para hierarquias de tipos (ex: eventos), usar `[JsonDerivedType]`:

```csharp
[JsonDerivedType(typeof(ContainerStartedEvent), "container-started")]
[JsonDerivedType(typeof(ContainerStoppedEvent), "container-stopped")]
public interface IContainerEvent { }
```

---

## 3. Padrão de DTOs

### 3.1 Nomenclatura

| Tipo | Sufixo | Exemplo |
|------|--------|---------|
| Request body | `Request` | `ContainerDeployRequest` |
| Response body | `Response` | `ContainerResponse` |
| Lista paginada | `PagedResponse<T>` | `PagedResponse<ContainerResponse>` |
| Evento interno | `Event` | `ContainerStartedEvent` |
| Resultado de operação | `Result` | `OperationResult` |

### 3.2 Estrutura obrigatória

```csharp
public sealed class ContainerDeployRequest
{
    public required string ImageName { get; init; }
    public required string Name { get; init; }
    public required double CpuLimit { get; init; }
    public required long MemoryLimitBytes { get; init; }
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
    public List<VolumeMapping> Volumes { get; init; } = [];
    public int InternalPort { get; init; }
}
```

Regras:
- Usar `required` em propriedades obrigatórias
- Usar `init` em DTOs de request (imutáveis após construção)
- Usar `sealed` em todos os DTOs
- Inicializar coleções com `= new()` ou `= []`
- **Nunca** usar `!` (null-forgiving operator) sem guarda prévia

### 3.3 Paginação

```csharp
public sealed class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public bool HasNextPage => Page * PageSize < TotalCount;
}
```

---

## 4. Tratamento de Nulos

### 4.1 Regras

- **Sempre** usar `??` e `?.` explicitamente
- **Nunca** usar `!` (null-forgiving) sem um `if (x is not null)` antes
- Preferir `is null` / `is not null` em vez de `== null` / `!= null`
- Em APIs Minimal, validar input com `Results.BadRequest()` antes de usar

```csharp
// ✅ CORRETO
var container = await _containerService.GetByIdAsync(id);
if (container is null)
    return Results.NotFound(new { error = "Container not found" });

// ❌ ERRADO
var container = await _containerService.GetByIdAsync(id);
return Results.Ok(container!); // crash em runtime se null
```

---

## 5. Entity Framework Core — Padrões AOT-Safe

### 5.1 Compiled Models

O projeto DEVE usar EF Core compiled models para compatibilidade AOT:

```bash
dotnet ef dbcontext optimize --output-dir CompiledModels --namespace Bzn.Cloudios.Infrastructure.CompiledModels
```

No `Program.cs`:

```csharp
builder.Services.AddDbContext<CloudiosDbContext>(options =>
    options.UseSqlite(connectionString)
           .UseModel(CloudiosDbContextModel.Instance)  // compiled model
           .UseSeeding(seedingAction));
```

### 5.2 Isolamento de Tenant — Extension Methods

**NÃO** usar `HasQueryFilter`. Usar extension methods nos repositórios:

```csharp
public static class ContainerQueryExtensions
{
    public static IQueryable<Container> ForRealm(this IQueryable<Container> query, Guid realmId)
        => query.Where(c => c.RealmId == realmId);
}
```

Uso:

```csharp
var containers = await _dbContext.Containers
    .ForRealm(realmId)
    .Where(c => c.Status == ContainerStatus.Running)
    .ToListAsync();
```

### 5.3 Queries — Sem eval, sem raw SQL dinâmico

- Usar LINQ com expressões estáticas
- **Nunca** `FromSqlRaw` com interpolação de string
- **Nunca** `ExecuteSqlRaw` com concatenação
- Parâmetros sempre via `FromSqlInterpolated` ou LINQ

---

## 6. Pacotes NuGet — Aprovação e Proibição

### 6.1 Aprovados (AOT-safe)

| Pacote | Uso |
|--------|-----|
| `Microsoft.EntityFrameworkCore.Sqlite` | ORM SQLite |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | Servir Blazor WASM |
| `Yarp.ReverseProxy` | Reverse proxy dinâmico |
| `System.Threading.Channels` | Event bus in-process |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Autenticação JWT |
| `Docker.DotNet` | Comunicação com Docker daemon (com JsonSerializerContext wrappers) |

### 6.2 Proibidos (não-AOT-safe)

| Pacote | Motivo |
|--------|--------|
| `MediatR` | Reflection para resolver handlers |
| `AutoMapper` | Reflection para mapeamento |
| `Scrutor` | Scan de assemblies |
| `Newtonsoft.Json` | Reflection pesada |
| `Serilog` com enrichers dinâmicos | Reflection |
| `Pomelo.EntityFrameworkCore.MySql` | Não necessário — usamos SQLite |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Não necessário — usamos SQLite |

---

## 7. Minimal APIs — Padrões

### 7.1 Estrutura de Endpoint

```csharp
public static class ContainerEndpoints
{
    public static void MapContainerEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/containers")
            .RequireAuthorization();

        group.MapGet("/", GetAllAsync);
        group.MapPost("/", DeployAsync);
        group.MapPost("/{id:guid}/start", StartAsync);
    }

    private static async Task<IResult> GetAllAsync(
        [FromServices] IContainerService service,
        [FromServices] ITenantProvider tenant,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await service.ListAsync(tenant.RealmId, search, page, pageSize);
        return Results.Ok(result);
    }
}
```

### 7.2 Registro no Program.cs

```csharp
app.MapContainerEndpoints();
app.MapAuthEndpoints();
app.MapBillingEndpoints();
app.MapRealmEndpoints();
```

---

## 8. Logging

Usar `ILogger<T>` com template strings (não interpolação):

```csharp
// ✅ CORRETO — Structured logging, AOT-safe
_logger.LogInformation("Container {ContainerId} started for realm {RealmId}", containerId, realmId);

// ❌ ERRADO — Interpolação gera allocation desnecessária e pode usar reflection
_logger.LogInformation($"Container {containerId} started for realm {realmId}");
```

---

## 9. Checklist de Revisão para IA

Antes de submeter qualquer código, o agente DEVE verificar:

- [ ] Nenhuma chamada a `System.Reflection`
- [ ] Nenhum uso de `dynamic`
- [ ] Todos os DTOs têm `JsonSerializerContext` registrado
- [ ] Todos os serviços registrados manualmente no DI
- [ ] Nenhum `!` sem guarda de nulo
- [ ] Queries EF Core usam extension methods para tenant isolation
- [ ] Logging usa template strings, não interpolação
- [ ] `dotnet publish -c Release -r linux-x64` passa sem erros
