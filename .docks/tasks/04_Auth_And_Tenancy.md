# Task 04: Autenticação JWT e Isolamento de Tenants

## Objetivo
Implementar o login via Minimal APIs, geração de JWT e garantir que nenhum Realm acesse dados de outro usando extension methods explícitos (não Global Query Filters).

## Requisitos

### 1. Endpoint de Autenticação
- `POST /api/auth/login`: Retorna JWT conforme definido em `API_CONTRACT.md`.
- Payload JWT deve conter claims: `UserId`, `RealmId`, `Role`.
- Configurar `JwtBearerHandler` no DI com validação de chave, issuer e audience.

### 2. ITenantProvider (Scoped)
Criar `ITenantProvider` que extrai o `RealmId` e `Role` do Token JWT da requisição HTTP atual:
```csharp
public interface ITenantProvider
{
    Guid RealmId { get; }
    string Role { get; }
    Guid UserId { get; }
}
```

### 3. Tenant Isolation — Extension Methods
Criar extension methods para cada entidade que possui `RealmId`:
```csharp
public static class ContainerQueryExtensions
{
    public static IQueryable<Container> ForRealm(this IQueryable<Container> query, Guid realmId)
        => query.Where(c => c.RealmId == realmId);
}
```
**Regra:** Toda query em entidades com `RealmId` DEVE chamar `.ForRealm(realmId)`. Nenhum `GlobalQueryFilter`.

### 4. Autorização via Policies
Criar policies baseadas nas Roles:
- `RequireGlobalAdmin`: Apenas `GlobalAdmin`
- `RequireRealmOwner`: `GlobalAdmin` ou `RealmOwner`
- `RequireRealmMember`: Qualquer role autenticado do Realm

### 5. JsonSerializerContext para Auth
Criar `AuthJsonContext` com `[JsonSerializable]` para `LoginRequest`, `LoginResponse`, `UserInfo`.

## Critérios de Aceite
* `POST /api/auth/login` retorna JWT válido para credenciais corretas.
* API retorna 401 para requisições sem JWT e 403 para Role insuficiente.
* Uma consulta à tabela `Containers` feita por um usuário traz apenas os dados do seu próprio Realm.
* Nenhum `HasQueryFilter` no DbContext — isolamento via extension methods.
