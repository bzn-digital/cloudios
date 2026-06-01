# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-06-01

### Added
- .NET 10 solution (`Bzn.Cloudios.slnx`) with 6 projects: Domain, Infrastructure, Application, WebAPI (Native AOT), WebApp (Blazor WASM), WebPlatform (Blazor WASM Admin)
- Native AOT configuration on WebAPI: `PublishAot=true`, reflection-free JSON serialization via `CloudiosJsonSerializerContext`
- Blazor WebAssembly standalone projects: WebApp (client panel) and WebPlatform (admin panel at `/admin`)
- NuGet packages: EF Core Sqlite, YARP Reverse Proxy, JWT Bearer, Docker.DotNet
- Domain DTOs (Auth, Realm, User, Container, Metric, Billing, Health, Error) and Enums (ContainerStatus, UserRole)
- Manual DI registration in WebAPI Program.cs
- Docker/Podman support for local testing: multi-stage `Dockerfile` (AOT publish), `compose.yaml`, `.dockerignore`
- WebAPI container runs as non-root user on port 8080 with Docker socket mounted
- Unit test project `Bzn.Cloudios.Tests` (xUnit) with Domain DTO/Enum tests
- PR validation workflow split into 3 jobs: Build, Unit Tests, AOT Publish
- `UserRole` enum: Platform roles (`PlatformAdmin`, `PlatformUser`, `PlatformSre`) for admin panel; Realm roles (`RealmOwner`, `RealmAdmin`, `RealmUser`, `RealmSre`) for client panel
- WebAPI serves both panels: WebApp (client, root) and WebPlatform (admin, `/admin`)
- SQLite database design with two DbContexts: `CloudiosDbContext` (main) and `MetricsDbContext` (metrics)
- Entities: Realm, User, Container, ContainerVolume, ContainerEnvVar, ContainerMetricHistory
- `SqlitePragmaInterceptor` applying WAL, NORMAL sync, foreign keys, and cache PRAGMAs on connection
- Fluent API configurations with CHECK constraints, unique indexes, CASCADE deletes per `DATABASE_SCHEMA.md`
- EF Core migrations `InitialCreate` for both contexts
- `DatabaseSeeder` creating system realm and PlatformAdmin user on first run
- Docker/Podman compose with named volume `cloudios-data` for persistent SQLite databases

## [Unreleased]

### Added
- JWT authentication: `POST /api/auth/login` endpoint with symmetric key validation
- `ITenantProvider` / `JwtTenantProvider`: extracts UserId, RealmId, Role from JWT claims (Scoped)
- `AuthService`: validates credentials and generates JWT with claims (UserId, RealmId, Role, Email)
- `TenantQueryExtensions`: `.ForRealm(realmId)` for Container, User, ContainerVolume, ContainerEnvVar (no Global Query Filters)
- Authorization policies: `RequirePlatformAdmin`, `RequirePlatformUser`, `RequireRealmOwner`, `RequireRealmMember`
- JWT Bearer configured with issuer, audience, signing key validation
- Realm CRUD: `GET /api/realms` (list/search/paginate), `GET /api/realms/{id}`, `POST`, `PUT`, `DELETE` (PlatformAdmin only)
- User CRUD: `GET /api/realms/{realmId}/users`, `POST`, `PUT`, `DELETE` (RealmOwner+)
- BCrypt password hashing (AOT-safe) for user creation and login verification
- Validations: unique email, unique realm name, no self-deletion, no last RealmOwner removal
- DatabaseSeeder updated to hash admin password with BCrypt
- Docker Client Service: AOT-safe Unix socket HTTP client (no Docker.DotNet reflection)
- `IContainerService` / `ContainerService`: Deploy, Start, Stop, Restart, Delete, GetContainerIp
- `DockerNetworkService`: ensures `cloudios_internal` network (172.20.0.0/16) on startup
- Container state synchronization: DB ↔ Docker (orphan cleanup, stale Running→Stopped fix)
- `ContainerCrudService`: CRUD + Docker lifecycle orchestration
- Container endpoints: `GET /api/realms/{realmId}/containers`, `POST`, `POST /{id}/deploy`, `POST /{id}/start|stop|restart`, `DELETE`
- Docker labels: `cloudios.realm`, `cloudios.container`, `cloudios.managed=true`
- CPU/RAM limits via HostConfig (CpuQuota, Memory)
- Container API refactored to `/api/containers` (realm from JWT, no realmId in URL)
- `GET /api/containers` — list with pagination, search, status filter (realm from JWT)
- `GET /api/containers/{id}` — detail with volumes and env vars
- `POST /api/containers` — create container (RealmOwner+)
- `POST /api/containers/{id}/deploy|start|stop|restart` — lifecycle (RealmOwner+)
- `DELETE /api/containers/{id}` — remove (RealmOwner only)
- `GET /api/containers/all` — admin list all containers across realms (PlatformAdmin)
- Validations: unique name per realm, imageName required, cpuLimitCores 0.1–4.0, memoryLimitBytes 128MB–8GB
- `IEventBus` / `InMemoryEventBus`: ContainerStarted, ContainerStopped, ContainerDeleted, ContainerFailed events
- `IEventBus` refactored with `Subscribe<TEvent>` for handler registration
- `InProcessEventBus` using `Channel<EventEnvelope>` bounded (capacity 1000, Wait back-pressure)
- `EventProcessorWorker` BackgroundService: consumes channel, dispatches to handlers in parallel, logs errors without crash
- `EventEnvelope` wrapper with EventType, Payload, EnqueuedAt
- `RealmBlockedEvent` added to domain events
- `EventJsonContext` with `[JsonSerializable]` for all events (logging future)
- `YarpRouteHandler` stub: AddRoute on Started, RemoveRoute on Stopped/Deleted
- `BillingEventHandler` stub: RegisterStart/Stop for billing calculation
- Handler subscriptions in Program.cs: YARP + Billing wired to ContainerStarted/Stopped/Deleted
- `IYarpRouteUpdater` interface + `YarpRouteUpdater` implementation (WebAPI)
- YARP configured with `InMemoryConfigProvider` for dynamic routes (empty initial routes)
- `ForwardedHeaders` configured for Cloudflare Tunnel (XForwardedFor, XForwardedProto)
- Hostname pattern: `{container-name}.{realm-slug}.cloudios.bzn.dev`
- `YarpRouteUpdater` subscribes to ContainerStarted/Stopped/Deleted events
- Route/Cluster added on container start, removed on stop/delete
- `Realm.Slug` property added for hostname generation
- YARP routes: hostname-based routing to container internal IP:port

### Deprecated

### Removed

### Fixed

### Security
