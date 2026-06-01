# Task 15: Blazor WASM — Layout, Auth State e Navegação (WebPlatform e WebApp)

## Objetivo
Construir a fundação de **dois frontends** Blazor WebAssembly distintos: o `Bzn.Cloudios.WebApp` (acesso dos clientes/Realms) e o `Bzn.Cloudios.WebPlatform` (acesso administrativo global). Estabelecer o gerenciamento de estado de autenticação, roteamento e comunicação base com a WebAPI para ambos.

## Requisitos

### 1. Estrutura dos Projetos Blazor WASM
- Certificar-se de que existem dois projetos frontend distintos: `Bzn.Cloudios.WebApp` e `Bzn.Cloudios.WebPlatform`.
- Configurar o `HttpClient` base em ambos os projetos apontando para a URL da `WebAPI`.
- Criar (ou centralizar em um projeto `Shared`) a lógica do `CustomAuthStateProvider` que:
  - Lê o JWT do `localStorage` do browser.
  - Decodifica as claims (UserId, RealmId, Role) sem usar reflection dinâmico (usar parser manual de JWT ou `System.Text.Json` com Source Generators).
  - Expõe o `AuthenticationState` atualizado para as aplicações.

### 2. Layout e Navegação do Cliente (`WebApp`)
- **Foco:** Usuários dos Realms (Owner, Dev, Viewer).
- Layout com sidebar colapsável à esquerda.
- Header contendo: Nome do usuário, nome do Realm atual e botão de logout.
- Itens da Sidebar:
  - Dashboard (Visão geral do Realm)
  - Services (Meus Apps)
  - Billing (Faturamento do Realm)
  - Team (Membros do Realm)

### 3. Layout e Navegação Administrativa (`WebPlatform`)
- **Foco:** Controle absoluto da PaaS (exclusivo para `GlobalAdmin`).
- Layout com sidebar colapsável à esquerda, preferencialmente com um esquema de cores diferente (ex: Dark Mode forçado) para distinguir visualmente do painel do cliente.
- Header contendo: Identificação do Admin e botão de logout.
- Itens da Sidebar:
  - Global Dashboard (Saúde do Host e Receita Total)
  - Realms (Gerenciamento de Clientes)
  - All Services (Visão global de todos os containers)
  - Settings (Configurações da PaaS)

### 4. Fluxo de Login e Autorização
- **Páginas de Login:** Criar formulário (email/senha) em ambos os apps consumindo `POST /api/auth/login`.
- **Proteção de Rota (WebPlatform):** O projeto administrativo deve rejeitar qualquer token que não possua a claim `Role = GlobalAdmin`. Se um cliente tentar logar aqui, exibir "Acesso Negado" (403) ou redirecionar para logout.
- **Proteção de Rota (WebApp):** O projeto cliente deve exigir autenticação (`[Authorize]`) e carregar os dados baseados no `RealmId` do token.
- Após login bem-sucedido e armazenamento do JWT no `localStorage`, redirecionar para o Dashboard correspondente.

### 5. Logout e Interceptação de Erros
- Implementar o fluxo de Logout (remover JWT do `localStorage` e redirecionar para a tela de login).
- Criar um `HttpInterceptor` ou `DelegatingHandler` customizado em ambos os projetos:
  - Injetar automaticamente o Header `Authorization: Bearer {token}` em todas as requisições para a API.
  - Se a WebAPI retornar HTTP 401 (Unauthorized), limpar o token localmente e redirecionar o usuário para o login de forma automática.

## Critérios de Aceite
* Os dois projetos Blazor rodam de forma independente e conseguem se autenticar consumindo a mesma WebAPI.
* O `WebPlatform` bloqueia ativamente o login ou o roteamento de usuários que não são `GlobalAdmin`.
* O cabeçalho de autenticação é enviado corretamente e de forma automática para as requisições da API.
* A expiração do token (retorno 401 da API) desconecta o usuário em ambos os sistemas sem gerar quebra de tela.