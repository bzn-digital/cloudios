# Task 02: Setup da Solução .NET 10 e Native AOT

## Objetivo
Criar a solução base do projeto com a estrutura de projetos definitiva, garantindo que a WebAPI suporte compilação Native AOT e que os painéis Blazor WebAssembly (Cliente e Admin) estejam configurados como projetos separados.

## Requisitos

### 1. Solution e Projetos
Criar a solution `Bzn.Cloudios.slnx` com os seguintes projetos na pasta `src/`:

| Projeto | Tipo | Namespace | AOT | Painel |
|---------|------|-----------|-----|--------|
| `Bzn.Cloudios.Domain` | Class Library | `Bzn.Cloudios.Domain` | Não publicado | — |
| `Bzn.Cloudios.Infrastructure` | Class Library | `Bzn.Cloudios.Infrastructure` | Não publicado | — |
| `Bzn.Cloudios.Application` | Class Library | `Bzn.Cloudios.Application` | Não publicado | — |
| `Bzn.Cloudios.WebAPI` | ASP.NET Core Minimal API | `Bzn.Cloudios.WebAPI` | **Sim** | — |
| `Bzn.Cloudios.WebApp` | Blazor WebAssembly App | `Bzn.Cloudios.WebApp` | Não (WASM) | **Cliente** |
| `Bzn.Cloudios.WebPlatform` | Blazor WebAssembly App | `Bzn.Cloudios.WebPlatform` | Não (WASM) | **Admin** |

### 2. Configuração AOT Obrigatória (WebAPI)
- Adicionar `<PublishAot>true</PublishAot>` no `.csproj` do `Bzn.Cloudios.WebAPI`.
- Configurar `System.Text.Json` para usar **Source Generators** obrigatórios (criar classes `JsonSerializerContext` para todos os DTOs definidos em `API_CONTRACT.md`).
- Adicionar `<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>` para garantir que nenhuma serialização por reflection ocorra.

### 3. Blazor WebAssembly Setup (WebApp — Painel do Cliente)
- Criar projeto Blazor WebAssembly standalone.
- Configurar `HttpClient` base apontando para a WebAPI.
- O WebAPI servirá os arquivos estáticos do WASM via `app.UseStaticFiles()` e `app.MapFallbackToFile("index.html")`.
- Roles de acesso: `RealmOwner`, `RealmAdmin`, `RealmUser`, `RealmSre`.

### 4. Blazor WebAssembly Setup (WebPlatform — Painel Administrativo)
- Criar projeto Blazor WebAssembly standalone separado do WebApp.
- Configurar `HttpClient` base apontando para a WebAPI.
- O WebAPI servirá os arquivos estáticos do admin em `/admin` via `StaticFileOptions` com `RequestPath="/admin"` e `MapFallbackToFile("/admin/{**path}", "admin/index.html")`.
- Roles de acesso: `PlatformAdmin`, `PlatformUser`, `PlatformSre`.

### 5. Injeção de Dependência Manual
- Registrar TODOS os serviços manualmente no `Program.cs` do WebAPI.
- Proibido: scan de assemblies, `AddAutoMapper`, `Scrutor`, ou qualquer registro dinâmico.

### 6. Referências entre Projetos
```
WebAPI → Application → Domain
WebAPI → Infrastructure → Domain
Application → Infrastructure (para interfaces)
WebApp → Domain (compartilha enums e DTOs)
WebPlatform → Domain (compartilha enums e DTOs)
WebAPI → WebApp (serving estático)
WebAPI → WebPlatform (serving estático /admin)
```

### 7. Pacotes NuGet Iniciais
- `Microsoft.EntityFrameworkCore.Sqlite` (Infrastructure)
- `Yarp.ReverseProxy` (WebAPI)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (WebAPI)
- `Docker.DotNet` (Infrastructure)

### 8. Docker/Podman para Testes Locais
- `Dockerfile` multi-stage: SDK para AOT publish, ASP.NET runtime para execução.
- `compose.yaml` compatível com Podman e Docker Compose.
- Container roda como non-root na porta 8080 com Docker socket montado.

### 9. Testes Unitários
- Projeto `Bzn.Cloudios.Tests` (xUnit) na pasta `tests/`.
- Testes de DTOs e Enums do Domain.
- `IsAotCompatible=true` no projeto de testes.

## Critérios de Aceite
* O comando `dotnet build` na solution compila sem erros.
* O comando `dotnet publish src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj -c Release -r linux-x64` gera um binário nativo isolado sem erros de reflection.
* O projeto Blazor WASM (WebApp) compila e pode ser servido pelo WebAPI via `UseStaticFiles`.
* O projeto Blazor WASM (WebPlatform) compila e pode ser servido pelo WebAPI em `/admin`.
* Os testes unitários passam (`dotnet test`).
* O `compose.yaml` sobe o container com sucesso.
