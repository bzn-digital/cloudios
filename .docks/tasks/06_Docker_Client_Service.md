# Task 06: Docker Client Service

## Objetivo
Construir o serviço de integração com o daemon do Docker Linux, implementando todas as operações de ciclo de vida de containers usando `Docker.DotNet` com wrappers AOT-safe.

## Requisitos

### 1. Integração Docker.DotNet
- Usar `Docker.DotNet` para comunicação com `/var/run/docker.sock`.
- Criar `JsonSerializerContext` wrappers para os tipos do Docker usados (`ContainerListResponse`, `ContainerCreateResponse`, `ContainerInspectResponse`, etc.).
- Se o AOT reclamar em tempo de compilação, fazer fallback para `HttpClient` chamando o socket Unix diretamente com serialização via Source Generators próprios.

### 2. Docker Network
- Criar rede Docker interna `cloudios_internal` (subnet `172.20.0.0/16`) na inicialização se não existir.
- Todos os containers de clientes são criados nesta rede.

### 3. Operações do Serviço
Implementar `IContainerService` com os métodos:
- `DeployAsync(ContainerDeployRequest request)` — Cria e inicia o container com:
  - Labels obrigatórios: `cloudios.realm={realmId}`, `cloudios.container={containerId}`, `cloudios.managed=true`
  - Limites de CPU e RAM via `HostConfig.CpuQuota`, `HostConfig.Memory`
  - Volumes mapeados (ver Task 12)
  - Variáveis de ambiente injetadas (ver Task 12)
  - Conexão na rede `cloudios_internal`
- `StartAsync(Guid containerId)` — Inicia container existente
- `StopAsync(Guid containerId)` — Para o container
- `RestartAsync(Guid containerId)` — Reinicia o processo
- `DeleteAsync(Guid containerId)` — Force stop + remove container + remove volumes órfãos
- `GetContainerIpAsync(string dockerContainerId)` — Retorna IP interno na rede cloudios_internal

### 4. Sincronização de Estado
- Ao iniciar, sincronizar o estado do banco com o Docker real:
  - Containers marcados como `Running` no DB mas parados no Docker → atualizar para `Stopped`
  - Containers no Docker com label `cloudios.managed` mas sem registro no DB → remover do Docker (órfãos)

## Critérios de Aceite
* `DeployAsync` cria um container real no Docker com labels e limites de recurso.
* `StopAsync` para o container e o comando `docker ps` confirma.
* `dotnet publish -c Release -r linux-x64` compila sem erros de AOT com Docker.DotNet.
