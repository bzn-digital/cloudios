# Task 07: Container API Endpoints

## Objetivo
Expor as operações de containers como Minimal APIs com validação, DTOs e autorização, conforme definido em `API_CONTRACT.md`.

## Requisitos

### 1. Endpoints de Container (por Realm)
Implementar endpoints conforme `API_CONTRACT.md`:
- `GET /api/containers` — Listar containers do Realm (com paginação, busca e filtro de status)
- `GET /api/containers/{id}` — Detalhes do container com volumes e env vars
- `POST /api/containers` — Deploy de novo container
- `POST /api/containers/{id}/start` — Iniciar container
- `POST /api/containers/{id}/stop` — Parar container
- `POST /api/containers/{id}/restart` — Reiniciar container
- `DELETE /api/containers/{id}` — Remover container e volumes

### 2. Endpoint Admin (Global)
- `GET /api/containers/all` — Listar todos os containers de todos os Realms com coluna `realmName`

### 3. Validações
- Nome do container único por Realm (409 Conflict se duplicado)
- `imageName` não pode ser vazio
- `cpuLimitCores` entre 0.1 e 4.0
- `memoryLimitBytes` entre 128MB e 8GB
- Apenas `RealmOwner` e `RealmDev` podem criar/iniciar/parar containers
- Apenas `RealmOwner` pode deletar containers do seu Realm
- `GlobalAdmin` pode operar sobre qualquer container

### 4. DTOs e JsonSerializerContext
Criar `ContainerJsonContext` com `[JsonSerializable]` para todos os DTOs de container definidos em `API_CONTRACT.md`.

### 5. Integração com Event Bus
Após operações bem-sucedidas de start/stop/delete, publicar eventos via `IEventBus`:
- `ContainerStartedEvent`
- `ContainerStoppedEvent`
- `ContainerDeletedEvent`
- `ContainerFailedEvent` (em caso de falha)

## Critérios de Aceite
* `POST /api/containers` cria um container e retorna 201 com o DTO completo.
* `GET /api/containers` retorna apenas containers do Realm do JWT.
* `DELETE /api/containers/{id}` por um RealmDev retorna 403.
* Nome duplicado no mesmo Realm retorna 409.
