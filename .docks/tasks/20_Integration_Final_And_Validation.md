# Task 20: Integração Final, Testes End-to-End e Validação AOT

## Objetivo
Realizar a integração final de todos os módulos, testar o fluxo de ponta a ponta da infraestrutura e garantir que o binário Native AOT (Alpine) compila sem warnings de *trimming* e funciona com máxima eficiência.

## Requisitos de Implementação

### 1. Validação AOT e Trimming
- Executar o build para o ambiente final: `dotnet publish src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj -c Release -r linux-musl-x64 --self-contained true`
- **Tolerância Zero:** Verificar rigorosamente se NENHUM *warning* de *trimming* (IL2026, etc.) ou AOT (IL3050, etc.) é gerado no console.
- Se houver warnings, corrigir a causa raiz usando Source Generators ou implementações AOT-safe. É estritamente proibido suprimir o erro com `<NoWarn>`.

### 2. Testes de Unidade Críticos (Lógica de Negócio)
Criar o projeto de testes `Bzn.Cloudios.Tests` (usando `xUnit` ou `NUnit`) focado na lógica pura, que não depende de I/O:
- `BillingServiceTests`: Validar cálculo exato de horas, frações e custo em Reais (R$).
- `JwtParserTests`: Garantir a extração correta das claims (`RealmId`, `Role`) sem reflection.
- `EventBusTests`: Validar se a publicação e consumo via `System.Threading.Channels` não gera deadlocks.

### 3. Teste de Integração End-to-End (E2E Black-Box)
Como o uso do `WebApplicationFactory` conflita com o Native AOT, o teste E2E deve ser feito com a infraestrutura real rodando. Executar manualmente (ou criar um script de automação com `Testcontainers`) o seguinte roteiro:

1. **Start:** Subir o `docker-compose.yml` final gerado na Task 19.
2. **Seed:** Confirmar que o banco `cloudios_main.db` gerou o `GlobalAdmin` inicial.
3. **Admin Login:** `POST /api/auth/login` (Admin) → Obter JWT.
4. **Setup:** `POST /api/realms` → Criar "Test Corp". Em seguida, criar um usuário `RealmOwner` para ele.
5. **Client Login:** `POST /api/auth/login` (Client) → Obter JWT do Realm.
6. **Deploy:** `POST /api/containers` → Enviar imagem `nginx:alpine`, porta 80, CPU 0.5, RAM 128MB.
7. **Verificação Host:** Rodar `docker ps` no host e confirmar se o container subiu com as labels `paas.realm=...`.
8. **Routing (YARP):** Fazer um request simulando o tráfego do Cloudflare e confirmar se o YARP faz o proxy corretamente para o IP interno do Nginx.
9. **Métricas e Billing:** Aguardar 2 minutos. Confirmar se o worker gravou dados no `cloudios_metrics.db` e se o endpoint `GET /api/billing/realm` retorna custo > R$ 0,00.
10. **Event System (Bloqueio):** Usar o admin para alterar o Realm para `IsBlocked = true`.
11. **Validação do EventBus:** Confirmar se o evento `RealmBlockedEvent` disparou e forçou o `Stop` no container Nginx automaticamente.
12. **Cleanup:** `DELETE /api/containers/{id}` → Verificar se o container e seus volumes atrelados foram completamente varridos do host.

### 4. Validação de Isolamento (Tenant Security)
- Confirmar que se um usuário do Realm A tentar listar (`GET`) ou deletar (`DELETE`) o ID de um container do Realm B, a API retorna `404 Not Found` (devido ao filtro de repositório) ou `403 Forbidden`.
- Confirmar que endpoints administrativos (`/api/billing/global`) rejeitam tokens de clientes.

### 5. Benchmark e Performance
- **Tamanho:** O binário AOT final (`Bzn.Cloudios.WebAPI`) deve ter menos de **40MB**.
- **Startup:** O tempo de inicialização da API (do comando run até o log "Application started") deve ser **inferior a 1 segundo**.
- **Consumo (Idle):** O container principal do Cloudios, em repouso absoluto, deve consumir **menos de 50MB de RAM**.

## Critérios de Aceite
- O projeto passa em 100% da compilação AOT sem exibir *Trimming/AOT Warnings*.
- O fluxo E2E (Login → Realm → Deploy → Metrics → Billing → Block → Delete) funciona impecavelmente no container final.
- Vazamento de dados entre clientes (Tenants) é matematicamente impossível pelas regras do repositório.
- A aplicação atinge todas as metas de baixo consumo exigidas para rodar em servidores limitados.