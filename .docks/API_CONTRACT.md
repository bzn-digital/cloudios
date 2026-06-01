# API Contract — BZN Cloudios

> Este documento define o contrato completo de todos os endpoints da WebAPI. Nenhum endpoint deve ser criado ou alterado sem estar documentado aqui. Todos os DTOs seguem o padrão definido em `AI_CODING_GUIDELINES.md`.

---

## 1. Convenções Gerais

### 1.1 URLs

- Base path: `/api`
- Formato: kebab-case (`/api/container-metrics`, `/api/auth/login`)
- IDs em rotas: GUID no formato string (`/{id:guid}`)

### 1.2 Autenticação

- Mecanismo: JWT Bearer Token no header `Authorization: Bearer {token}`
- Endpoints públicos: apenas `/api/auth/login` e `/health`

### 1.3 Status Codes

| Código | Significado | Quando usar |
|--------|-------------|-------------|
| 200 | OK | GET e POST bem-sucedidos |
| 201 | Created | Recurso criado via POST |
| 204 | No Content | DELETE bem-sucedido ou ação sem retorno |
| 400 | Bad Request | Validação de input falhou |
| 401 | Unauthorized | Token ausente ou inválido |
| 403 | Forbidden | Role insuficiente para a operação |
| 404 | Not Found | Recurso não encontrado |
| 409 | Conflict | Conflito (ex: nome de container já existe) |
| 500 | Internal Server Error | Erro inesperado |

### 1.4 Erro Padronizado

```json
{
  "type": "https://cloudios.bzn.dev/errors/{error-type}",
  "title": "Brief human-readable message",
  "status": 400,
  "detail": "Specific details about what went wrong"
}
```

### 1.5 Paginação

Query params: `?page=1&pageSize=20&search=optional`

Response envelope:

```json
{
  "items": [...],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "hasNextPage": true
}
```

---

## 2. Auth

### 2.1 POST `/api/auth/login`

**Auth:** Nenhuma (público)

**Request:**
```json
{
  "email": "admin@bzn.dev",
  "password": "string"
}
```

**Response 200:**
```json
{
  "token": "eyJhbGciOi...",
  "expiresAt": "2026-06-01T15:00:00Z",
  "user": {
    "id": "guid",
    "email": "admin@bzn.dev",
    "role": "GlobalAdmin",
    "realmId": "guid",
    "realmName": "system"
  }
}
```

**Response 401:** Credenciais inválidas

---

## 3. Realms

### 3.1 GET `/api/realms`

**Auth:** `GlobalAdmin`

**Query:** `?page=1&pageSize=20&search=optional`

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Acme Corp",
      "isActive": true,
      "createdAt": "2026-01-15T10:00:00Z",
      "userCount": 3,
      "containerCount": 5
    }
  ],
  "totalCount": 10,
  "page": 1,
  "pageSize": 20,
  "hasNextPage": false
}
```

### 3.2 GET `/api/realms/{id:guid}`

**Auth:** `GlobalAdmin`

**Response 200:**
```json
{
  "id": "guid",
  "name": "Acme Corp",
  "isActive": true,
  "createdAt": "2026-01-15T10:00:00Z",
  "users": [
    {
      "id": "guid",
      "email": "user@acme.com",
      "role": "RealmOwner",
      "isBlocked": false
    }
  ]
}
```

### 3.3 POST `/api/realms`

**Auth:** `GlobalAdmin`

**Request:**
```json
{
  "name": "Acme Corp"
}
```

**Response 201:**
```json
{
  "id": "guid",
  "name": "Acme Corp",
  "isActive": true,
  "createdAt": "2026-06-01T13:00:00Z"
}
```

### 3.4 PUT `/api/realms/{id:guid}`

**Auth:** `GlobalAdmin`

**Request:**
```json
{
  "name": "Acme Corp Updated",
  "isActive": true
}
```

**Response 200:** Mesmo formato do GET individual

### 3.5 DELETE `/api/realms/{id:guid}`

**Auth:** `GlobalAdmin`

**Response 204:** Realm e todos os dados associados removidos (CASCADE)

---

## 4. Users

### 4.1 GET `/api/realms/{realmId:guid}/users`

**Auth:** `GlobalAdmin` ou `RealmOwner` (do mesmo Realm)

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "email": "dev@acme.com",
      "role": "RealmDev",
      "isBlocked": false,
      "createdAt": "2026-02-01T08:00:00Z"
    }
  ],
  "totalCount": 3,
  "page": 1,
  "pageSize": 20,
  "hasNextPage": false
}
```

### 4.2 POST `/api/realms/{realmId:guid}/users`

**Auth:** `GlobalAdmin` ou `RealmOwner` (do mesmo Realm)

**Request:**
```json
{
  "email": "dev@acme.com",
  "password": "string",
  "role": "RealmDev"
}
```

**Response 201:**
```json
{
  "id": "guid",
  "email": "dev@acme.com",
  "role": "RealmDev",
  "isBlocked": false,
  "createdAt": "2026-06-01T13:00:00Z"
}
```

### 4.3 PUT `/api/realms/{realmId:guid}/users/{id:guid}`

**Auth:** `GlobalAdmin` ou `RealmOwner` (do mesmo Realm)

**Request:**
```json
{
  "role": "RealmViewer",
  "isBlocked": true
}
```

**Response 200:** Mesmo formato do GET

### 4.4 DELETE `/api/realms/{realmId:guid}/users/{id:guid}`

**Auth:** `GlobalAdmin`

**Response 204**

---

## 5. Containers

### 5.1 GET `/api/containers`

**Auth:** Qualquer role autenticado. Retorna apenas containers do Realm do usuário.

**Query:** `?page=1&pageSize=20&search=optional&status=Running`

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "web-api",
      "imageName": "nginx:latest",
      "internalPort": 80,
      "status": "Running",
      "cpuLimitCores": 0.5,
      "memoryLimitBytes": 536870912,
      "costPerHourBRL": 0.02,
      "currentMonthCostBRL": 14.40,
      "startedAtUtc": "2026-05-30T10:00:00Z",
      "createdAt": "2026-05-01T08:00:00Z"
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20,
  "hasNextPage": false
}
```

### 5.2 GET `/api/containers/{id:guid}`

**Auth:** Qualquer role do Realm do container

**Response 200:**
```json
{
  "id": "guid",
  "name": "web-api",
  "imageName": "nginx:latest",
  "internalPort": 80,
  "status": "Running",
  "cpuLimitCores": 0.5,
  "memoryLimitBytes": 536870912,
  "costPerHourBRL": 0.02,
  "currentMonthCostBRL": 14.40,
  "dockerContainerId": "abc123...",
  "startedAtUtc": "2026-05-30T10:00:00Z",
  "createdAt": "2026-05-01T08:00:00Z",
  "volumes": [
    {
      "id": "guid",
      "hostPath": "/data/acme/web-api/data",
      "containerPath": "/app/data",
      "isReadOnly": false
    }
  ],
  "environmentVariables": [
    {
      "id": "guid",
      "key": "ASPNETCORE_ENVIRONMENT",
      "value": "Production"
    }
  ]
}
```

### 5.3 POST `/api/containers`

**Auth:** `RealmOwner` ou `RealmDev` (do mesmo Realm)

**Request:**
```json
{
  "name": "web-api",
  "imageName": "nginx:latest",
  "internalPort": 80,
  "cpuLimitCores": 0.5,
  "memoryLimitBytes": 536870912,
  "costPerHourBRL": 0.02,
  "volumes": [
    {
      "hostPath": "/data/acme/web-api/data",
      "containerPath": "/app/data",
      "isReadOnly": false
    }
  ],
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Production",
    "API_KEY": "secret-value"
  }
}
```

**Response 201:**
```json
{
  "id": "guid",
  "name": "web-api",
  "imageName": "nginx:latest",
  "internalPort": 80,
  "status": "Deploying",
  "cpuLimitCores": 0.5,
  "memoryLimitBytes": 536870912,
  "costPerHourBRL": 0.02,
  "currentMonthCostBRL": 0.0,
  "startedAtUtc": null,
  "createdAt": "2026-06-01T13:00:00Z",
  "volumes": [...],
  "environmentVariables": [...]
}
```

### 5.4 POST `/api/containers/{id:guid}/start`

**Auth:** `RealmOwner` ou `RealmDev`

**Response 200:**
```json
{
  "id": "guid",
  "status": "Running",
  "dockerContainerId": "abc123...",
  "startedAtUtc": "2026-06-01T13:05:00Z"
}
```

### 5.5 POST `/api/containers/{id:guid}/stop`

**Auth:** `RealmOwner` ou `RealmDev`

**Response 200:**
```json
{
  "id": "guid",
  "status": "Stopped",
  "dockerContainerId": "abc123..."
}
```

### 5.6 POST `/api/containers/{id:guid}/restart`

**Auth:** `RealmOwner` ou `RealmDev`

**Response 200:** Mesmo formato do start

### 5.7 DELETE `/api/containers/{id:guid}`

**Auth:** `RealmOwner` (do mesmo Realm) ou `GlobalAdmin`

**Response 204:** Container e volumes Docker removidos

### 5.8 GET `/api/containers/{id:guid}/logs`

**Auth:** Qualquer role do Realm do container

**Query:** `?tail=100` (últimas N linhas)

**Response 200:**
```json
{
  "containerId": "guid",
  "logs": [
    {
      "timestamp": "2026-06-01T13:05:01Z",
      "stream": "stdout",
      "line": "Server started on port 80"
    },
    {
      "timestamp": "2026-06-01T13:05:02Z",
      "stream": "stderr",
      "line": "Warning: low disk space"
    }
  ]
}
```

### 5.9 GET `/api/containers/all` (Admin)

**Auth:** `GlobalAdmin` apenas

Lista **todos** os containers de **todos** os Realms, com coluna adicional `realmName`.

**Query:** `?page=1&pageSize=20&search=&realmId=&status=`

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "realmId": "guid",
      "realmName": "Acme Corp",
      "name": "web-api",
      "imageName": "nginx:latest",
      "status": "Running",
      "cpuLimitCores": 0.5,
      "memoryLimitBytes": 536870912,
      "costPerHourBRL": 0.02,
      "currentMonthCostBRL": 14.40
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20,
  "hasNextPage": true
}
```

---

## 6. Metrics

### 6.1 GET `/api/containers/{id:guid}/metrics`

**Auth:** Qualquer role do Realm do container

**Query:** `?from=2026-05-30T00:00:00Z&to=2026-06-01T00:00:00Z&interval=5m`

**Response 200:**
```json
{
  "containerId": "guid",
  "from": "2026-05-30T00:00:00Z",
  "to": "2026-06-01T00:00:00Z",
  "dataPoints": [
    {
      "timestamp": "2026-05-30T00:05:00Z",
      "cpuPercent": 12.5,
      "memoryUsedBytes": 268435456,
      "networkRxBytes": 1024000,
      "networkTxBytes": 512000
    }
  ]
}
```

### 6.2 GET `/api/metrics/host` (Admin)

**Auth:** `GlobalAdmin`

Retorna métricas agregadas do host físico.

**Response 200:**
```json
{
  "totalCpuPercent": 45.2,
  "totalMemoryUsedBytes": 4294967296,
  "totalMemoryTotalBytes": 8589934592,
  "activeContainers": 12,
  "diskUsedBytes": 107374182400,
  "diskTotalBytes": 214748364800
}
```

---

## 7. Billing

### 7.1 GET `/api/billing/realm`

**Auth:** Qualquer role autenticado. Retorna do Realm do JWT.

**Query:** `?month=2026-06` (padrão: mês atual)

**Response 200:**
```json
{
  "realmId": "guid",
  "realmName": "Acme Corp",
  "month": "2026-06",
  "totalCostBRL": 86.40,
  "services": [
    {
      "containerId": "guid",
      "containerName": "web-api",
      "costPerHourBRL": 0.02,
      "runningHours": 720.0,
      "totalCostBRL": 14.40
    }
  ]
}
```

### 7.2 GET `/api/billing/global`

**Auth:** `GlobalAdmin`

**Query:** `?month=2026-06`

**Response 200:**
```json
{
  "month": "2026-06",
  "totalRevenueBRL": 432.00,
  "realms": [
    {
      "realmId": "guid",
      "realmName": "Acme Corp",
      "totalCostBRL": 86.40,
      "containerCount": 5,
      "activeContainerCount": 3
    }
  ]
}
```

---

## 8. Health

### 8.1 GET `/health`

**Auth:** Nenhuma (público — para Cloudflare Tunnel health check)

**Response 200:**
```json
{
  "status": "Healthy",
  "version": "1.0.0",
  "uptime": "12:30:45"
}
```
