# Task 18: Blazor WASM — Painel Admin (Realm e Container Manager)

## Objetivo
Construir as páginas de gerenciamento de Realms e containers globais para o GlobalAdmin.

## Requisitos

### 1. Gerenciador de Realms
- **Tabela** listando todos os Realms (chama `GET /api/realms`).
- **Colunas:** Nome, Status (ativo/bloqueado), Nº de Usuários, Nº de Containers, Criado em.
- **Ações por linha:**
  - "Editar" — Abre modal para alterar nome e status (IsActive)
  - "Bloquear/Desbloquear" — Toggle de `IsActive`. Ao bloquear, publica `RealmBlockedEvent` que pausa containers do Realm.
  - "Deletar" — Com confirmação dupla (digitar nome do Realm)
- **Botão "Novo Realm"** — Modal com campo de nome.

### 2. Detalhes do Realm
- Ao clicar no nome do Realm, navegar para página de detalhes.
- Exibe: informações do Realm, lista de Usuários, lista de Containers.
- **Gerenciamento de Usuários:**
  - Tabela de usuários do Realm
  - Botão "Adicionar Usuário" — Modal com email, senha e role
  - Ações: alterar role, bloquear/desbloquear, remover

### 3. Visão "Deus" de Containers
- **Tabela** listando **todos** os containers de **todos** os Realms (chama `GET /api/containers/all`).
- **Colunas:** Realm (nome), Nome, Imagem, Status, CPU Limit, RAM Limit, Custo Mensal (R$).
- **Filtros:** Por Realm (dropdown), Por Status, Busca por nome.
- **Ações por linha:** Start, Stop, Restart, Delete — para intervenção de emergência da administração.
- Estas ações chamam os mesmos endpoints de container (Task 07) mas com privilégio de GlobalAdmin.

## Critérios de Aceite
* Admin consegue visualizar todos os serviços rodando no host via UI.
* Admin consegue parar o container de um cliente específico com sucesso.
* Bloquear um Realm pausa os containers daquele Realm.
* Gerenciamento de usuários dentro de um Realm funciona.
