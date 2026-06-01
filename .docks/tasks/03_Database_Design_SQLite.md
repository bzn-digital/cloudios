# Task 03: Design do Banco de Dados — SQLite (Main + Metrics)

## Objetivo
Implementar os dois bancos SQLite do Cloudios com o schema completo definido em `DATABASE_SCHEMA.md`, configurando o EF Core com compiled models para compatibilidade AOT.

## Requisitos

### 1. Dois DbContexts

| DbContext | Banco | Tabelas |
|-----------|-------|---------|
| `CloudiosDbContext` | `cloudios_main.db` | Realms, Users, Containers, ContainerVolumes, ContainerEnvVars |
| `MetricsDbContext` | `cloudios_metrics.db` | ContainerMetrics_History |

### 2. PRAGMAs via Interceptor
Criar `SqlitePragmaInterceptor` que executa na primeira conexão:
```sql
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA busy_timeout=5000;
PRAGMA cache_size=-64000;
PRAGMA temp_store=MEMORY;
PRAGMA foreign_keys=ON;
```

### 3. Entity Configuration (Fluent API)
Configurar todas as entidades segundo o schema de `DATABASE_SCHEMA.md`:
- Tipos de coluna (TEXT para GUID e DateTime, INTEGER para boolean, REAL para decimal)
- CHECK constraints para enums (Role, Status)
- Todos os índices definidos no schema
- CASCADE deletes nas FKs
- Unique indexes (Realms.Name, Users.Email, ContainerEnvVars.ContainerId+Key)

### 4. Compiled Models
Gerar compiled models para ambos os DbContexts:
```bash
dotnet ef dbcontext optimize --output-dir CompiledModels --namespace Bzn.Cloudios.Infrastructure.CompiledModels --context CloudiosDbContext
dotnet ef dbcontext optimize --output-dir CompiledModelsMetrics --namespace Bzn.Cloudios.Infrastructure.CompiledModels --context MetricsDbContext
```

Registrar no `Program.cs`:
```csharp
options.UseSqlite(connectionString).UseModel(CloudiosDbContextModel.Instance);
options.UseSqlite(connectionString).UseModel(MetricsDbContextModel.Instance);
```

### 5. Migrações
Gerar a migração inicial `InitialCreate` para ambos os contextos.

### 6. Seeding
Na primeira execução, criar:
- Realm `system` (interno da administração)
- User `GlobalAdmin` com email/senha das variáveis de ambiente `ADMIN_EMAIL` e `ADMIN_PASSWORD`

## Critérios de Aceite
* `dotnet ef database update` cria ambos os arquivos `.db` localmente.
* As PRAGMAs estão ativas (verificar com `PRAGMA journal_mode`).
* Índices e constraints estão presentes no banco gerado.
* `dotnet publish -c Release -r linux-x64` compila sem erros (compiled models AOT-safe).
