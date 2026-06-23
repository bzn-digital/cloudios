# 🛠️ Diretrizes de Governança e Git Workflow do Projeto

Você deve seguir estritamente as regras de gerenciamento e o fluxo de Git descritos abaixo. Nenhuma alteração de código deve ser feita fora deste padrão.

## 1. Fonte Única de Verdade para Demandas
- Todas as tarefas, requisitos e bugs estão documentados EXCLUSIVAMENTE nas **Issues do GitHub**.
- É **proibido** buscar tarefas em arquivos locais (como TODOs) ou tentar adivinhar o próximo passo. Acesse sempre a API do GitHub para ler a Issue solicitada.

## 2. Padrão de Git Workflow (Obrigatório)

A branch `main` é protegida e nunca deve receber código direto. O fluxo de desenvolvimento segue a árvore abaixo:

### Fase 1: Desenvolvimento de Funcionalidades / Correções
1. Toda nova tarefa deve ser iniciada a partir da branch **`development`** (garanta que ela está atualizada com o último `pull`).
2. A nova branch deve OBRIGATORIAMENTE seguir o padrão de nomenclatura baseado no tipo da Issue:
   - `feat/nome-da-task` (Para novas funcionalidades)
   - `bug/nome-da-task` ou `fix/nome-da-task` (Para correção de bugs em desenvolvimento)
   - `enhancement/nome-da-task` (Para melhorias em código existente)
   - `hotfix/nome-da-task` (Para correções críticas vindas direto da main)
3. Após finalizar o código e testar localmente, faça o `push` da sua branch e **abra um Pull Request (PR) direcionado para a branch `development`**.

### Fase 2: Preparação de Release (Apenas quando solicitado explicitamente)
Quando o usuário solicitar o fechamento de uma versão ou o deploy:
1. Crie uma nova branch a partir de **`development`** seguindo o padrão de versionamento semântico:
   - `release/vX.X.X` (Exemplo: `release/v1.0.0`)
2. Atualize a versão no arquivo `package.json` dentro desta branch de release.
3. Abra um **Pull Request da branch `release/vX.X.X` direcionado para a branch `main`**.

### Fase 3: Publicação e Tags
Após o PR da release ser aprovado e mergeado na `main`:
1. Crie uma **Tag Git** na `main` com o mesmo número da versão (Ex: `v1.0.0`).
2. Utilize a API do GitHub para **criar uma Release oficial** no repositório, utilizando o texto da tag e listando brevemente o que foi entregue.
3. Faça o merge de volta da `main` (ou da branch de release) para a `development` para garantir que os arquivos de versão (`package.json`) fiquem sincronizados em todo o projeto.

---
*Nota: Falhar em seguir qualquer uma dessas etapas resultará no cancelamento da tarefa pelo usuário. Leia este documento antes de iniciar qualquer chat.*