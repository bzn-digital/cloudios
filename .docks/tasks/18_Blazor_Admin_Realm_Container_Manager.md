# Task 18: Blazor WASM (`WebPlatform`) — Painel Admin (Gerenciamento de Realms e Visão Global)

## Objetivo
Construir as páginas de controle operacional para o `GlobalAdmin` no projeto `Bzn.Cloudios.WebPlatform`: o gerenciamento completo de Realms (clientes) e a Visão "Deus" (God Mode), que permite intervir em qualquer container hospedado no servidor.

## Requisitos de Implementação

### 1. Gerenciador de Realms (Tabela Principal)
- **Listagem:** Chama `GET /api/realms` para listar todos os clientes cadastrados.
- **Colunas:** Nome, Status (Ativo/Bloqueado via badge), Nº de Usuários, Nº de Containers ativos, Data de Criação.
- **Ações por linha (Botões):**
  - **Editar:** Abre modal para alterar nome.
  - **Bloquear/Desbloquear:** Toggle de status (`IsBlocked`). *Nota de Arquitetura:* Ao chamar o endpoint de bloqueio, o backend deve publicar o `RealmBlockedEvent` no Event Bus, que por sua vez deve invalidar os tokens JWT ativos daquele Realm e disparar comandos de `Stop` em todos os containers do cliente.
  - **Deletar:** Exige confirmação dupla (digitar o nome exato do Realm). Remove o cliente, usuários e deleta todos os containers atrelados.
- **Botão "Novo Realm":** Abre um modal simples com o nome do cliente. O backend deve criar o Realm e gerar o ambiente isolado.

### 2. Página de Detalhes do Realm (`/realms/{id}`)
- Ao clicar no nome do Realm na tabela, o admin é direcionado para esta visão aprofundada.
- **Abas de Navegação:**
  - **Informações Gerais:** Resumo do consumo financeiro e de recursos deste cliente específico.
  - **Containers:** Lista (somente leitura para o admin) dos apps rodando neste Realm.
  - **Gerenciamento de Usuários:**
    - Tabela de usuários vinculados ao Realm.
    - **Ações:** Alterar Role (ex: promover de Viewer para Dev), Bloquear acesso de um usuário específico, ou Remover.
    - **Botão "Adicionar Usuário":** Modal contendo Email, Senha Inicial e Role. 

### 3. Visão "Deus" de Containers (Global Container Manager)
- **Tabela Global:** Chama `GET /api/containers/all`. *Atenção:* A API no backend precisará ignorar o `TenantProvider` padrão se a role for `GlobalAdmin`, permitindo uma listagem sem filtro de isolamento.
- **Colunas Visíveis:** Nome do Realm (Cliente), Nome do Container, Imagem, Status, Limite CPU, Limite RAM, Custo Acumulado (R$).
- **Filtros e Buscas:**
  - Dropdown para filtrar por Realm específico.
  - Dropdown para filtrar por Status (`Running`, `Stopped`, etc).
  - Campo de busca por nome do container.
- **Ações de Emergência:**
  - Botões idênticos aos do cliente (`Start`, `Stop`, `Restart`, `Delete`), porém as requisições partem do Painel Administrativo.
  - Exemplo de uso: Se um container de um cliente estiver consumindo muita banda ou travando o disco, o admin pode vir aqui e forçar um `Stop` manual.

## Critérios de Aceite
* O Painel bloqueia o acesso a qualquer usuário que não seja `GlobalAdmin`.
* A ação de "Bloquear Realm" paralisa imediatamente (Stop) todos os containers daquele cliente no servidor host Docker.
* A "Visão Deus" lista os containers de diferentes clientes na mesma tabela, identificando corretamente a qual Realm eles pertencem.
* Ações manuais de interrupção ou deleção de um container a partir da "Visão Deus" funcionam e refletem instantaneamente no status real do Docker no Linux.