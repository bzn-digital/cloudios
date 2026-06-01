# Event System — BZN Cloudios

> Este documento define o sistema de eventos internos do Cloudios. O event bus garante desacoplamento entre módulos (Docker, YARP, Billing, Metrics) sem uso de reflection, sendo 100% AOT-safe.

---

## 1. Interface do Event Bus

```csharp
namespace Bzn.Cloudios.Application.Events;

public interface IEventBus
{
    /// Publishes an event to all registered subscribers.
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : notnull;

    /// Subscribes a handler for a specific event type.
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler) where TEvent : notnull;
}
```

### 1.1 Implementação — InProcessEventBus

Usa `Channel<T>` do `System.Threading.Channels` internamente.

**Características:**
- Bounded channel com capacidade de 1000 eventos (back-pressure)
- Um `BackgroundService` (`EventProcessorWorker`) consome o channel e despacha para handlers
- Handlers são registrados via `Subscribe<TEvent>()` no `Program.cs`
- Publicação é fire-and-forget — o publisher não espera o processamento
- Ordem de processamento: FIFO dentro do mesmo tipo de evento

### 1.2 Registro no DI

```csharp
// Program.cs
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
builder.Services.AddHostedService<EventProcessorWorker>();
```

### 1.3 Registro de Handlers

```csharp
// Program.cs — Após build do host
var eventBus = app.Services.GetRequiredService<IEventBus>();

eventBus.Subscribe<ContainerStartedEvent>(async (e, ct) =>
{
    var yarpUpdater = app.Services.GetRequiredService<IYarpRouteUpdater>();
    await yarpUpdater.AddRouteAsync(e.ContainerId, e.InternalIp, e.InternalPort, ct);
});

eventBus.Subscribe<ContainerStartedEvent>(async (e, ct) =>
{
    var billingService = app.Services.GetRequiredService<IBillingService>();
    await billingService.RegisterStartAsync(e.ContainerId, e.StartedAtUtc, ct);
});
```

---

## 2. Eventos do Domínio

### 2.1 ContainerStartedEvent

Publicado quando um container é iniciado com sucesso no Docker.

```csharp
public sealed class ContainerStartedEvent
{
    public required Guid ContainerId { get; init; }
    public required Guid RealmId { get; init; }
    public required string DockerContainerId { get; init; }
    public required string InternalIp { get; init; }      // ex: "172.18.0.5"
    public required int InternalPort { get; init; }        // ex: 8080
    public required string Hostname { get; init; }         // ex: "app.acme.cloudios.bzn.dev"
    public required DateTimeOffset StartedAtUtc { get; init; }
}
```

**Publicadores:** `ContainerService` (Application)
**Assinantes:** `YarpRouteUpdater` (Infrastructure), `BillingService` (Application)

---

### 2.2 ContainerStoppedEvent

Publicado quando um container é parado com sucesso.

```csharp
public sealed class ContainerStoppedEvent
{
    public required Guid ContainerId { get; init; }
    public required Guid RealmId { get; init; }
    public required string DockerContainerId { get; init; }
    public required string Hostname { get; init; }
    public required DateTimeOffset StoppedAtUtc { get; init; }
}
```

**Publicadores:** `ContainerService` (Application)
**Assinantes:** `YarpRouteUpdater` (Infrastructure), `BillingService` (Application)

---

### 2.3 ContainerDeletedEvent

Publicado quando um container e seus recursos são removidos.

```csharp
public sealed class ContainerDeletedEvent
{
    public required Guid ContainerId { get; init; }
    public required Guid RealmId { get; init; }
    public required string DockerContainerId { get; init; }
    public required string Hostname { get; init; }
    public required DateTimeOffset DeletedAtUtc { get; init; }
}
```

**Publicadores:** `ContainerService` (Application)
**Assinantes:** `YarpRouteUpdater` (Infrastructure)

---

### 2.4 ContainerFailedEvent

Publicado quando uma operação Docker falha (container crash, imagem não encontrada, etc.).

```csharp
public sealed class ContainerFailedEvent
{
    public required Guid ContainerId { get; init; }
    public required Guid RealmId { get; init; }
    public required string Operation { get; init; }        // "Start", "Stop", "Deploy"
    public required string ErrorMessage { get; init; }
    public required DateTimeOffset FailedAtUtc { get; init; }
}
```

**Publicadores:** `ContainerService` (Application)
**Assinantes:** Logging/Alerting (futuro)

---

### 2.5 RealmBlockedEvent

Publicado quando um Realm é bloqueado pelo GlobalAdmin.

```csharp
public sealed class RealmBlockedEvent
{
    public required Guid RealmId { get; init; }
    public required bool IsBlocked { get; init; }
    public required DateTimeOffset ChangedAtUtc { get; init; }
}
```

**Publicadores:** `RealmService` (Application)
**Assinantes:** `ContainerService` (Application) — para pausar containers do Realm bloqueado

---

## 3. Fluxo de Eventos — Diagrama

```
ContainerService.StartAsync()
  │
  ├── Docker API: criar/iniciar container
  ├── Atualizar status no DB (cloudios_main.db)
  └── _eventBus.PublishAsync(ContainerStartedEvent)
        │
        ├── YarpRouteUpdater: adiciona rota dinâmica
        │     hostname → internalIp:internalPort
        │
        └── BillingService: registra início de cobrança
              containerId + startedAtUtc

ContainerService.StopAsync()
  │
  ├── Docker API: parar container
  ├── Atualizar status no DB
  └── _eventBus.PublishAsync(ContainerStoppedEvent)
        │
        ├── YarpRouteUpdater: remove rota dinâmica
        │
        └── BillingService: registra fim de cobrança
              containerId + stoppedAtUtc → calcula horas

ContainerService.DeleteAsync()
  │
  ├── Docker API: force stop + remove volumes
  ├── Remover registro do DB
  └── _eventBus.PublishAsync(ContainerDeletedEvent)
        │
        └── YarpRouteUpdater: remove rota dinâmica
```

---

## 4. Serialização AOT-Safe

Todos os eventos precisam de `JsonSerializerContext` para logging persistente (futuro):

```csharp
[JsonSerializable(typeof(ContainerStartedEvent))]
[JsonSerializable(typeof(ContainerStoppedEvent))]
[JsonSerializable(typeof(ContainerDeletedEvent))]
[JsonSerializable(typeof(ContainerFailedEvent))]
[JsonSerializable(typeof(RealmBlockedEvent))]
public partial class EventJsonContext : JsonSerializerContext;
```

Polimorfismo para o channel interno:

```csharp
// O channel transporta envelopes tipados — sem polimorfismo no channel
internal sealed class EventEnvelope
{
    public required Type EventType { get; init; }
    public required object Payload { get; init; }
    public required DateTimeOffset PublishedAtUtc { get; init; }
}
```

**Nota:** O `EventEnvelope` é interno e usado apenas no `InProcessEventBus`. Não precisa de serialização JSON pois nunca sai do processo.
