# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- .NET 10 solution (`Bzn.Cloudios.slnx`) with 5 projects: Domain, Infrastructure, Application, WebAPI (Native AOT), WebApp (Blazor WASM)
- Native AOT configuration on WebAPI: `PublishAot=true`, reflection-free JSON serialization via `CloudiosJsonSerializerContext`
- Blazor WebAssembly standalone project with HttpClient configured for WebAPI
- NuGet packages: EF Core Sqlite, YARP Reverse Proxy, JWT Bearer, Docker.DotNet
- Domain DTOs (Auth, Realm, User, Container, Metric, Billing, Health, Error) and Enums (ContainerStatus, UserRole)
- Manual DI registration in WebAPI Program.cs
- Docker/Podman support for local testing: multi-stage `Dockerfile` (AOT publish), `compose.yaml`, `.dockerignore`
- WebAPI container runs as non-root user on port 8080 with Docker socket mounted
- Unit test project `Bzn.Cloudios.Tests` (xUnit) with Domain DTO/Enum tests
- PR validation workflow split into 3 jobs: Build, Unit Tests, AOT Publish

### Changed

### Deprecated

### Removed

### Fixed

### Security
