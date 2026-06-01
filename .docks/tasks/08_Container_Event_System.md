# Task 08: Container Event System

## Objetivo
Implementar o sistema de eventos internos do Cloudios usando `System.Threading.Channels`, permitindo desacoplamento entre os módulos de Docker, YARP e Billing.

## Requisitos

### 1. IEventBus e InProcessEventBus
Implementar conforme `EVENT_SYSTEM.md`:
- `IEventBus` com métodos `PublishAsync<TEvent>` e `Subscribe<TEvent>`
- `InProcessEventBus` usando `Channel<EventEnvelope>` bounded (capacidade 1000)
- Registro no DI como Singleton

### 2. EventProcessorWorker
Criar `BackgroundService` que:
- Consome eventos do channel continuamente
- Despacha cada evento para os handlers registrados
- Loga erros de handler sem crashar o worker
- Usa `ChannelBoundedMode.Wait` para back-pressure

### 3. Eventos do Domínio
Implementar todos os eventos definidos em `EVENT_SYSTEM.md`:
- `ContainerStartedEvent`
- `ContainerStoppedEvent`
- `ContainerDeletedEvent`
- `ContainerFailedEvent`
- `RealmBlockedEvent`

### 4. JsonSerializerContext
Criar `EventJsonContext` com `[JsonSerializable]` para todos os eventos (necessário para logging futuro).

### 5. Registro de Handlers no Program.cs
Estruturar o registro de handlers de forma que fique claro qual módulo assina qual evento:
```csharp
// YARP handlers
eventBus.Subscribe<ContainerStartedEvent>(yarpUpdater.AddRouteAsync);
eventBus.Subscribe<ContainerStoppedEvent>(yarpUpdater.RemoveRouteAsync);
eventBus.Subscribe<ContainerDeletedEvent>(yarpUpdater.RemoveRouteAsync);

// Billing handlers
eventBus.Subscribe<ContainerStartedEvent>(billingService.RegisterStartAsync);
eventBus.Subscribe<ContainerStoppedEvent>(billingService.RegisterStopAsync);
```

## Critérios de Aceite
* `PublishAsync` não bloqueia o chamador (fire-and-forget).
* Múltiplos handlers para o mesmo evento são executados em paralelo.
* Se um handler falha, os outros continuam e o erro é logado.
* `dotnet publish -c Release -r linux-x64` compila sem erros.
