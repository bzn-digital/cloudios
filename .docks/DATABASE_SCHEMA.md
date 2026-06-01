# Database Schema — BZN Cloudios

> Este documento define o schema SQL completo dos dois bancos SQLite do Cloudios. Qualquer alteração no schema DEVE ser refletida aqui antes de ser implementada no código.

---

## 1. Visão Geral — Dois Bancos

| Banco | Arquivo | Propósito | DbContext |
|-------|---------|-----------|-----------|
| Main | `cloudios_main.db` | Dados transacionais (Realms, Users, Containers) | `CloudiosDbContext` |
| Metrics | `cloudios_metrics.db` | Série temporal de telemetria | `MetricsDbContext` |

---

## 2. cloudios_main.db

### 2.1 Tabela `Realms`

```sql
CREATE TABLE Realms (
    Id         TEXT    NOT NULL PRIMARY KEY,  -- GUID como TEXT (36 chars)
    Name       TEXT    NOT NULL CHECK(length(Name) <= 100),
    IsActive   INTEGER NOT NULL DEFAULT 1,   -- SQLite não tem BOOLEAN nativo
    CreatedAt  TEXT    NOT NULL               -- ISO 8601 string
);

CREATE UNIQUE INDEX IX_Realms_Name ON Realms (Name);
```

### 2.2 Tabela `Users`

```sql
CREATE TABLE Users (
    Id           TEXT    NOT NULL PRIMARY KEY,
    RealmId      TEXT    NOT NULL,
    Email        TEXT    NOT NULL CHECK(length(Email) <= 256),
    PasswordHash TEXT    NOT NULL,
    Role         TEXT    NOT NULL CHECK(Role IN ('GlobalAdmin', 'RealmOwner', 'RealmDev', 'RealmViewer')),
    IsBlocked    INTEGER NOT NULL DEFAULT 0,
    CreatedAt    TEXT    NOT NULL,

    FOREIGN KEY (RealmId) REFERENCES Realms(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
CREATE INDEX IX_Users_RealmId ON Users (RealmId);
CREATE INDEX IX_Users_RealmId_Role ON Users (RealmId, Role);
```

### 2.3 Tabela `Containers`

```sql
CREATE TABLE Containers (
    Id                TEXT    NOT NULL PRIMARY KEY,
    RealmId           TEXT    NOT NULL,
    Name              TEXT    NOT NULL CHECK(length(Name) <= 100),
    DockerContainerId TEXT    NULL,              -- NULL antes do deploy
    ImageName         TEXT    NOT NULL,
    InternalPort      INTEGER NOT NULL DEFAULT 8080,
    Status            TEXT    NOT NULL DEFAULT 'Stopped' CHECK(Status IN ('Running', 'Stopped', 'Failed', 'Deploying')),
    CpuLimitCores     REAL    NOT NULL DEFAULT 0.5,
    MemoryLimitBytes  INTEGER NOT NULL DEFAULT 536870912,  -- 512MB
    CostPerHourBRL    REAL    NOT NULL DEFAULT 0.02,
    StartedAtUtc      TEXT    NULL,              -- Timestamp do último start
    CreatedAt         TEXT    NOT NULL,

    FOREIGN KEY (RealmId) REFERENCES Realms(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Containers_RealmId ON Containers (RealmId);
CREATE INDEX IX_Containers_RealmId_Status ON Containers (RealmId, Status);
CREATE INDEX IX_Containers_DockerContainerId ON Containers (DockerContainerId);
CREATE INDEX IX_Containers_Name ON Containers (Name);
```

### 2.4 Tabela `ContainerVolumes`

```sql
CREATE TABLE ContainerVolumes (
    Id             TEXT    NOT NULL PRIMARY KEY,
    ContainerId    TEXT    NOT NULL,
    HostPath       TEXT    NOT NULL,
    ContainerPath  TEXT    NOT NULL,
    IsReadOnly     INTEGER NOT NULL DEFAULT 0,

    FOREIGN KEY (ContainerId) REFERENCES Containers(Id) ON DELETE CASCADE
);

CREATE INDEX IX_ContainerVolumes_ContainerId ON ContainerVolumes (ContainerId);
```

### 2.5 Tabela `ContainerEnvVars`

```sql
CREATE TABLE ContainerEnvVars (
    Id           TEXT    NOT NULL PRIMARY KEY,
    ContainerId  TEXT    NOT NULL,
    Key          TEXT    NOT NULL CHECK(length(Key) <= 256),
    Value        TEXT    NOT NULL,

    FOREIGN KEY (ContainerId) REFERENCES Containers(Id) ON DELETE CASCADE
);

CREATE INDEX IX_ContainerEnvVars_ContainerId ON ContainerEnvVars (ContainerId);
CREATE UNIQUE INDEX IX_ContainerEnvVars_ContainerId_Key ON ContainerEnvVars (ContainerId, Key);
```

---

## 3. cloudios_metrics.db

### 3.1 Tabela `ContainerMetrics_History`

```sql
CREATE TABLE ContainerMetrics_History (
    Id               INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ContainerId      TEXT    NOT NULL,           -- GUID do container (não FK — banco diferente)
    Timestamp        TEXT    NOT NULL,           -- ISO 8601 UTC
    CpuPercent       REAL    NOT NULL DEFAULT 0,
    MemoryUsedBytes  INTEGER NOT NULL DEFAULT 0,
    NetworkRxBytes   INTEGER NOT NULL DEFAULT 0,
    NetworkTxBytes   INTEGER NOT NULL DEFAULT 0,
    BlockReadBytes   INTEGER NOT NULL DEFAULT 0,
    BlockWriteBytes  INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IX_Metrics_ContainerId_Timestamp ON ContainerMetrics_History (ContainerId, Timestamp);
CREATE INDEX IX_Metrics_Timestamp ON ContainerMetrics_History (Timestamp);
```

### 3.2 Estratégia de Limpeza

Rotina diária (03:00 UTC) que executa:

```sql
DELETE FROM ContainerMetrics_History
WHERE Timestamp < datetime('now', '-90 days');
```

Após o DELETE, executar:

```sql
INSERT INTO ContainerMetrics_History(ContainerMetrics_History) VALUES ('optimize');
-- ou: PRAGMA optimize;
```

---

## 4. Configuração Comum — PRAGMAs

Ambos os bancos devem ter estas PRAGMAs aplicadas na conexão:

```sql
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA busy_timeout=5000;
PRAGMA cache_size=-64000;     -- 64MB
PRAGMA temp_store=MEMORY;
PRAGMA foreign_keys=ON;
```

Aplicadas via `DbCommandInterceptor` no EF Core ou na string de conexão:

```
Data Source=cloudios_main.db;Mode=ReadWriteCreate;Cache=Shared;
```

---

## 5. Estratégia de Migrações — AOT

EF Core migrations são compatíveis com AOT quando usamos **compiled models**:

```bash
# Gerar migração
dotnet ef migrations add InitialCreate --context CloudiosDbContext --project src/Bzn.Cloudios.Infrastructure

# Gerar compiled models
dotnet ef dbcontext optimize --output-dir CompiledModels --namespace Bzn.Cloudios.Infrastructure.CompiledModels
```

No `Program.cs`:

```csharp
builder.Services.AddDbContext<CloudiosDbContext>(options =>
    options.UseSqlite(mainConnectionString)
           .UseModel(CloudiosDbContextModel.Instance));

builder.Services.AddDbContext<MetricsDbContext>(options =>
    options.UseSqlite(metricsConnectionString)
           .UseModel(MetricsDbContextModel.Instance));
```

---

## 6. Seeding Inicial

Na primeira execução, o sistema deve criar:

1. **Realm padrão:** `system` — Realm interno da administração
2. **User GlobalAdmin:** Email e senha definidos por variáveis de ambiente (`ADMIN_EMAIL`, `ADMIN_PASSWORD`)
3. **PRAGMAs:** Aplicadas automaticamente via interceptor

---

## 7. Relacionamentos — Diagrama Textual

```
Realms (1) ──────< (N) Users
  │
  └──────────────< (N) Containers
                      │
                      ├──< (N) ContainerVolumes
                      │
                      ├──< (N) ContainerEnvVars
                      │
                      └── [cloudios_metrics.db]
                           ContainerMetrics_History.ContainerId → Containers.Id
                           (Sem FK — banco separado)
```
