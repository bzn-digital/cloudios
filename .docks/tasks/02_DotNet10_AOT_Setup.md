# Task 02: Setup da Solução .NET 10 e Native AOT

## Objetivo
Criar a solução base do projeto com a estrutura de projetos definitiva, garantindo que a WebAPI suporte compilação Native AOT e que o Blazor WebAssembly esteja configurado como projeto separado.

## Requisitos

### 1. Solution e Projetos
Criar a solution `Bzn.Cloudios.sln` com os seguintes projetos na pasta `src/`:

| Projeto | Tipo | Namespace | AOT |
|---------|------|-----------|-----|
| `Bzn.Cloudios.Domain` | Class Library | `Bzn.Cloudios.Domain` | Não publicado |
| `Bzn.Cloudios.Infrastructure` | Class Library | `Bzn.Cloudios.Infrastructure` | Não publicado |
| `Bzn.Cloudios.Application` | Class Library | `Bzn.Cloudios.Application` | Não publicado |
| `Bzn.Cloudios.WebAPI` | ASP.NET Core Minimal API | `Bzn.Cloudios.WebAPI` | **Sim** |
| `Bzn.Cloudios.WebApp` | Blazor WebAssembly App | `Bzn.Cloudios.WebApp` | Não (WASM) |

### 2. Configuração AOT Obrigatória (WebAPI)
- Adicionar `<PublishAot>true</PublishAot>` no `.csproj` do `Bzn.Cloudios.WebAPI`.
- Configurar `System.Text.Json` para usar **Source Generators** obrigatórios (criar classes `JsonSerializerContext` para todos os DTOs definidos em `API_CONTRACT.md`).
- Adicionar `<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>` para garantir que nenhuma serialização por reflection ocorra.

### 3. Blazor WebAssembly Setup (WebApp)
- Criar projeto Blazor WebAssembly standalone.
- Configurar `HttpClient` base apontando para a WebAPI.
- O WebAPI servirá os arquivos estáticos do WASM via `app.UseStaticFiles()` e `app.MapFallbackToFile("index.html")`.

### 4. Injeção de Dependência Manual
- Registrar TODOS os serviços manualmente no `Program.cs` do WebAPI.
- Proibido: scan de assemblies, `AddAutoMapper`, `Scrutor`, ou qualquer registro dinâmico.

### 5. Referências entre Projetos
```
WebAPI → Application → Domain
WebAPI → Infrastructure → Domain
Application → Infrastructure (para interfaces)
WebApp → Domain (compartilha enums e DTOs)
```

### 6. Pacotes NuGet Iniciais
- `Microsoft.EntityFrameworkCore.Sqlite` (Infrastructure)
- `Yarp.ReverseProxy` (WebAPI)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (WebAPI)
- `System.Threading.Channels` (Application)
- `Docker.DotNet` (Infrastructure)
- `Microsoft.AspNetCore.Components.WebAssembly.Server` (WebAPI)

## Critérios de Aceite
* O comando `dotnet build` na solution compila sem erros.
* O comando `dotnet publish src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj -c Release -r linux-x64` gera um binário nativo isolado sem erros de reflection.
* O projeto Blazor WASM compila e pode ser servido pelo WebAPI via `UseStaticFiles`.
