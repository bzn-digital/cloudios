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

### Deprecated

### Removed

### Fixed

### Security
