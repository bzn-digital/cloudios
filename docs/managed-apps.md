# Managed Apps - Documentação

## Visão Geral

Managed Apps são aplicações containerizadas gerenciadas automaticamente pelo Cloudios. Cada instância é um container Docker/Podman com isolamento de recursos, monitoramento e bilhetagem integrados.

## Padrões de Nomenclatura

### Container Names

Os containers de managed apps seguem o padrão:

```
cma-{realm_short_id}-{instance_name}
```

- **cma**: Prefixo identificador de "Cloudios Managed App"
- **realm_short_id**: Primeiros 4 caracteres do ID do realm (formato hexadecimal sem hífens)
- **instance_name**: Nome da instância sanitizado (lowercase, sem espaços, apenas alfanuméricos e hífens)

**Exemplos:**
- `cma-a1b2-portainer`
- `cma-3d4e-redis-cache`
- `cma-5f6g-postgres-db`

### Networks

Cada realm possui sua própria rede Docker isolada:

```
cloudios_{realm_id}
```

- **cloudios**: Prefixo padrão
- **realm_id**: ID completo do realm (formato hexadecimal sem hífens)

**Exemplo:**
- `cloudios_17d840595461483cad1c17a347567ab2`

### Volume Paths

Os volumes de dados são armazenados em:

```
{volumes_base_path}/managed-apps/{instance_id}
```

- **volumes_base_path**: Configurável via `Volumes:BasePath` (padrão: `~/cloudios`)
- **instance_id**: ID completo da instância (formato hexadecimal sem hífens)

**Exemplo:**
- `/home/user/cloudios/managed-apps/d7cc40be71144205a181d81204b5bccc`

## Port Mapping

- **Porta interna**: Definida pelo template da aplicação (ex: 9000 para Portainer)
- **Porta host**: Alocada automaticamente pelo Cloudios no range 2000-4500
- **Mapeamento**: `{host_port}:{internal_port}/tcp`

## Labels

Todos os containers de managed apps possuem labels para identificação:

```json
{
  "cloudios.realm": "{realm_id}",
  "cloudios.managed-app": "{instance_id}",
  "cloudios.managed": "true"
}
```

## Comunicação entre Containers

### Container-to-Container
Containers na mesma rede se comunicam usando o nome do container como hostname:

```
http://cma-a1b2-portainer:9000
```

### Host-to-Container
Acesso externo via host usando a porta mapeada:

```
http://localhost:2000
```

## Ciclo de Vida

### Status

- **Imaging**: Container está sendo criado e imagem está sendo baixada
- **Initializing**: Container foi criado e está iniciando
- **Running**: Container está rodando normalmente
- **Stopped**: Container foi parado
- **Failed**: Container falhou durante criação ou execução
- **Terminated**: Container foi deletado

### Operações

- **Create**: Cria uma nova instância de managed app
- **Start**: Inicia um container parado ou cria se não existir
- **Stop**: Para um container em execução
- **Restart**: Reinicia um container em execução
- **Delete**: Remove o container e seus volumes

## Resource Limits

Cada instância possui limites de recursos configurados pelo tamanho escolhido:

- **CPU**: Limitado em nano-CPUs (1 core = 1.000.000.000 nanoCPUs)
- **Memory**: Limitado em bytes
- **Cost**: Calculado por hora com base nos recursos alocados

## Segurança

- Isolamento por realm (multi-tenancy)
- Limites estritos de recursos
- Redes isoladas por realm
- Volumes persistentes por instância
