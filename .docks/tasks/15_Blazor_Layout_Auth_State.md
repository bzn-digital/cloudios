# Task 15: Blazor WASM — Layout, Auth State e Navegação

## Objetivo
Construir a fundação do frontend Blazor WebAssembly: layout compartilhado, gerenciamento de estado de autenticação, roteamento por role e integração com a WebAPI.

## Requisitos

### 1. Projeto Blazor WASM
- Projeto `Bzn.Cloudios.WebApp` já criado na Task 02.
- Configurar `HttpClient` base apontando para a WebAPI.
- Implementar `CustomAuthStateProvider` que:
  - Lê o JWT do `localStorage` do browser
  - Decodifica as claims (UserId, RealmId, Role) sem reflection (usar parser manual de JWT ou `System.Text.Json` com Source Generators)
  - Expõe `AuthenticationState` com claims e role

### 2. Layout Principal
- Layout com sidebar colapsável à esquerda
- Header com nome do usuário, realm e botão de logout
- Sidebar com navegação condicional por role:
  - **Client (RealmOwner/Dev/Viewer):** Dashboard, Services, Billing
  - **Admin (GlobalAdmin):** Dashboard Global, Realms, All Services, Billing Global

### 3. Página de Login
- Formulário com email e senha
- Chama `POST /api/auth/login`
- Armazena JWT no `localStorage`
- Redireciona para a página correta com base na role

### 4. Autorização por Rota
- Páginas admin usam `[Authorize(Roles = "GlobalAdmin")]`
- Páginas client usam `[Authorize]`
- Página de login é pública

### 5. Logout
- Remove JWT do `localStorage`
- Redireciona para login

### 6. Interceptor de 401
- Se qualquer chamada à API retorna 401, limpar o token e redirecionar para login automaticamente.

## Critérios de Aceite
* Login funcional — JWT armazenado e usuário autenticado no WASM.
* Sidebar mostra menus diferentes para GlobalAdmin vs RealmDev.
* Acesso direto a URL admin por um client redireciona para login ou retorna 403.
* Logout limpa o token e redireciona.
