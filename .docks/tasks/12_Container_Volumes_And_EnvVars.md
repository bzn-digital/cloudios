# Task 12: Container Volumes, Environment Variables e Port Mapping

## Objetivo
Implementar o suporte completo a volumes Docker persistentes, variáveis de ambiente e mapeamento de portas internas para os containers dos clientes.

## Requisitos

### 1. Container Volumes
- Na criação do container (Task 06 `DeployAsync`), mapear volumes definidos no `ContainerDeployRequest`.
- Cada volume especifica: `HostPath`, `ContainerPath`, `IsReadOnly`.
- **Padrão de HostPath:** `/var/lib/cloudios/volumes/realm-{realmId}/container-{containerId}/{subpath}`
- Garantir que o diretório no host existe antes do deploy (`Directory.CreateDirectory`).
- Volumes são persistidos na tabela `ContainerVolumes` (cloudios_main.db).

### 2. Environment Variables
- Na criação do container, injetar variáveis de ambiente definidas no `ContainerDeployRequest`.
- Formato: `Dictionary<string, string>` → convertido para `List<string>` no formato `KEY=VALUE` para o Docker.
- Variáveis são persistidas na tabela `ContainerEnvVars` (cloudios_main.db).
- **Segurança:** Env vars contendo secrets NÃO são retornadas no DTO de response para usuários `RealmViewer` — apenas `RealmOwner` e `GlobalAdmin` veem os valores.

### 3. Internal Port Mapping
- Cada container define `InternalPort` (porta em que a aplicação dentro do container escuta).
- O YARP proxy roteia para `http://{containerIp}:{internalPort}`.
- Padrão: 8080 se não especificado.

### 4. Atualização de Configuração
- `PUT /api/containers/{id}/env-vars` — Atualizar variáveis de ambiente (requer restart do container para aplicar)
- `PUT /api/containers/{id}/volumes` — Adicionar/remover volumes (requer redeploy)
- Estas operações atualizam o banco E requerem ação no Docker.

### 5. Cleanup de Volumes no Delete
- Quando um container é deletado (Task 06 `DeleteAsync`):
  - Remover registros das tabelas `ContainerVolumes` e `ContainerEnvVars`
  - Opcionalmente remover diretório de volumes do host (flag `removeVolumes` no request de delete, padrão `true`)

## Critérios de Aceite
* Container criado com volumes monta corretamente os paths no host.
* Variáveis de ambiente estão acessíveis dentro do container (`docker exec` confirma).
* Deletar um container remove os registros de volumes e env vars do banco.
* RealmViewer não vê valores de env vars secretas no response.
