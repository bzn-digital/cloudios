# Task 05: Gerenciamento de Realms e Usuários

## Objetivo
Implementar o CRUD completo de Realms e Usuários, permitindo que o GlobalAdmin gerencie clientes e que RealmOwners gerenciem membros do seu Realm.

## Requisitos

### 1. Realm CRUD (GlobalAdmin apenas)
Implementar endpoints conforme `API_CONTRACT.md`:
- `GET /api/realms` — Listar realms com paginação e busca
- `GET /api/realms/{id}` — Detalhes de um realm com seus usuários
- `POST /api/realms` — Criar novo realm
- `PUT /api/realms/{id}` — Atualizar nome e status (IsActive)
- `DELETE /api/realms/{id}` — Remover realm e todos os dados associados (CASCADE)

### 2. User CRUD
Implementar endpoints conforme `API_CONTRACT.md`:
- `GET /api/realms/{realmId}/users` — Listar usuários do realm (GlobalAdmin ou RealmOwner do mesmo Realm)
- `POST /api/realms/{realmId}/users` — Criar usuário no realm
- `PUT /api/realms/{realmId}/users/{id}` — Atualizar role e status (IsBlocked)
- `DELETE /api/realms/{realmId}/users/{id}` — Remover usuário (GlobalAdmin apenas)

### 3. Validações
- Email único global (não pode haver dois usuários com o mesmo email)
- Nome de Realm único
- Proibir auto-exclusão (GlobalAdmin não pode deletar a si mesmo)
- Proibir remoção do último RealmOwner de um Realm

### 4. Hashing de Senha
- Usar `BCrypt` ou `PBKDF2` para hash de senhas (AOT-safe).
- Nunca armazenar senhas em texto plano.

### 5. JsonSerializerContext
Criar `RealmJsonContext` e `UserJsonContext` com todos os DTOs de request/response.

## Critérios de Aceite
* GlobalAdmin consegue criar, editar e desativar Realms.
* RealmOwner consegue adicionar e gerenciar usuários no seu Realm.
* Email duplicado retorna 409 Conflict.
* Um RealmDev não consegue acessar endpoints de gerenciamento de usuários (403).
