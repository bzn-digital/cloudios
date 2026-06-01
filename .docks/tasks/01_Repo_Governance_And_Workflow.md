# Task 01: Governança do Repositório, Licença e GitHub Automations

## Objetivo
Configurar a estrutura raiz do repositório `cloudios`, definindo a licença AGPLv3, o fluxo de versionamento (Git Flow) e implementando toda a esteira de automações do GitHub (CI, Templates, Labels e Dependabot).

## Requisitos de Implementação

### 1. Licença e Changelog
- **Licença (AGPLv3):** Criar o arquivo `LICENSE` na raiz com o texto integral da GNU Affero General Public License v3.0.
- **Changelog:** Criar o arquivo `CHANGELOG.md` na raiz seguindo o padrão [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/). Estruturar as seções iniciais (`[Unreleased]`, `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`).

### 2. Git Workflow (Branching Model)
- **Branches Protegidas:**
  - `main`: Código de produção. Requer PR, aprovação de review e status checks passando.
  - `development`: Ambiente base de integração. Requer PR.
- **Padrão de Nomenclatura:** `feature/add-container-volumes`, `fix/metrics-batch-insert`, `hotfix/jwt-expiry-bug`, `release/v*`.

### 3. Templates do GitHub (`.github/` folder)
- **Pull Request Template:** Criar `.github/pull_request_template.md` contendo:
  - Descrição das mudanças.
  - Tipo de PR (Bugfix, Feature, Refatoração, etc.).
  - Issue relacionada (Closes #Issue).
  - Checklist: testes locais, AOT build passando, sem uso de reflection.

### 4. GitHub Actions (Workflows de Automação)
Criar os seguintes workflows na pasta `.github/workflows/`:
- **PR Validation (`pr-validation.yml`):**
  - Ao abrir um PR para `main` ou `development`, rodar `dotnet build` e `dotnet publish -c Release -r linux-x64` no projeto WebAPI para garantir que o código compila em AOT.
  - *Status Check* obrigatório antes do merge.
- **Auto-Labeler (`pr-labeler.yml`):**
  - Identificar automaticamente o tipo de branch (`feature/`, `fix/`, `hotfix/`, `release/`) e aplicar as *labels* correspondentes no Pull Request.
- **Auto-Release (`release-drafter.yml`):**
  - Disparado quando uma Tag semântica for criada.
  - Gerar automaticamente uma *GitHub Release* atrelada à Tag.

### 5. Dependabot
- Criar o arquivo `.github/dependabot.yml`.
- Ecossistema `nuget` checando a cada semana.
- Ecossistema `docker` checando a cada semana.
- Atribuir automaticamente a label `dependencies` aos PRs gerados.

## Critérios de Aceite
* A pasta `.github` possui todos os templates e workflows configurados em formato YAML.
* A abertura de um PR com origem em uma branch `feature/X` recebe a label correta automaticamente.
* O arquivo `dependabot.yml` é válido e reconhecido pelo GitHub.
* O `CHANGELOG.md` e a licença AGPLv3 estão na raiz.
* O README.md reflete AGPLv3 (não BSL).
