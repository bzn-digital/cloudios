# Task 20: Integração Final, Testes End-to-End e Validação AOT

## Objetivo
Realizar a integração final de todos os módulos, testar o fluxo completo de ponta a ponta e garantir que o binário Native AOT compila e funciona corretamente em produção.

## Requisitos

### 1. Validação AOT Final
- Executar `dotnet publish src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj -c Release -r linux-x64`
- Verificar que NENHUM warning de trimming ou AOT é gerado
- Se houver warnings, corrigir a causa raiz (não suprimir com `<NoWarn>`)

### 2. Teste de Fluxo Completo
Executar manualmente (ou via script) o seguinte fluxo:

1. **Setup:** Iniciar o Cloudios (via Docker Compose ou binário direto)
2. **Seed:** Verificar que o GlobalAdmin padrão foi criado
3. **Login:** `POST /api/auth/login` com credenciais do admin → obter JWT
4. **Criar Realm:** `POST /api/realms` → Realm "Test Corp"
5. **Criar Usuário:** `POST /api/realms/{id}/users` → RealmOwner para "Test Corp"
6. **Login como RealmOwner:** Obter JWT do cliente
7. **Deploy Container:** `POST /api/containers` → nginx:latest com porta 80
8. **Verificar Docker:** `docker ps` mostra o container com labels `cloudios.*`
9. **Verificar YARP:** Request para `nginx.test-corp.cloudios.bzn.dev` é proxyado para o container
10. **Verificar Métricas:** Após 60s, `cloudios_metrics.db` contém registros
11. **Verificar Billing:** `GET /api/billing/realm` retorna custo > 0
12. **Parar Container:** `POST /api/containers/{id}/stop`
13. **Verificar YARP:** Rota removida, request retorna 404/502
14. **Deletar Container:** `DELETE /api/containers/{id}` → container removido do Docker
15. **Bloquear Realm:** `PUT /api/realms/{id}` com `isActive=false`
16. **Verificar Isolamento:** Login como RealmOwner do Realm bloqueado → containers parados

### 3. Testes Unitários (Mínimos)
Criar projeto de testes `Bzn.Cloudios.Tests` com:
- Teste de `BillingService` — cálculo de horas e custo
- Teste de `ITenantProvider` — extração de claims do JWT
- Teste de `ContainerQueryExtensions.ForRealm()` — isolamento de tenant
- Teste de `InProcessEventBus` — publicação e consumo de eventos

Framework: `xUnit` com `Microsoft.AspNetCore.Mvc.Testing` para testes de integração da API.

### 4. Verificação de Segurança
- Confirmar que Realm A não acessa containers de Realm B
- Confirmar que RealmDev não deleta containers
- Confirmar que JWT expirado retorna 401
- Confirmar que `/health` é acessível sem autenticação

### 5. Verificação de Performance
- Binário AOT final: medir tamanho e tempo de startup
- Target: binário < 50MB, startup < 2s
- RAM em idle: < 100MB

## Critérios de Aceite
* `dotnet publish -c Release -r linux-x64` compila sem erros e sem warnings de AOT.
* Fluxo completo de ponta a ponta funciona: login → criar realm → deploy → métricas → billing → stop → delete.
* Isolamento de tenants é verificado — nenhum vazamento de dados.
* Testes unitários passam.
* Binário AOT cumpre os targets de performance.
