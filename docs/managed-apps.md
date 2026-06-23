---
title: Managed Apps
layout: default
---

# Managed Apps - Documentação

Managed Apps são aplicações containerizadas gerenciadas automaticamente pelo Cloudios. Cada instância é um container Docker/Podman com isolamento de recursos, monitoramento e bilhetagem integrados.

## Índice

- [Padrões de Nomenclatura](#padrões-de-nomenclatura)
- [Networks](#networks)
- [Volume Paths](#volume-paths)
- [Port Mapping](#port-mapping)
- [Labels](#labels)
- [Comunicação entre Containers](#comunicação-entre-containers)
- [Ciclo de Vida](#ciclo-de-vida)
- [Operações](#operações)
- [Resource Limits](#resource-limits)
- [Segurança](#segurança)

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

**Exemplo:**
```
0.0.0.0:2000->9000/tcp
```

Neste exemplo, a porta 9000 do container é acessível via porta 2000 no host.

## Labels

Todos os containers de managed apps possuem labels para identificação:

```json
{
  "cloudios.realm": "{realm_id}",
  "cloudios.managed-app": "{instance_id}",
  "cloudios.managed": "true"
}
```

Esses labels permitem filtrar e identificar containers gerenciados pelo Cloudios.

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

> **Importante:** Use sempre a porta interna (9000) para comunicação entre containers na mesma rede. Use a porta do host (2000) apenas para acesso externo.

## Ciclo de Vida

### Status

- **Imaging**: Container está sendo criado e imagem está sendo baixada
- **Initializing**: Container foi criado e está iniciando
- **Running**: Container está rodando normalmente
- **Stopped**: Container foi parado
- **Failed**: Container falhou durante criação ou execução
- **Terminated**: Container foi deletado

## Operações

### Create
Cria uma nova instância de managed app. O sistema aloca automaticamente uma porta disponível e enfileira o deploy.

### Start
Inicia um container parado ou cria se não existir. Se o container não existe, ele é criado e iniciado.

### Stop
Para um container em execução. O container permanece no sistema e pode ser reiniciado posteriormente.

### Restart
Reinicia um container em execução. Útil para aplicar configurações ou recuperar de erros temporários.

### Delete
Remove o container e seus volumes. Esta operação é irreversível e todos os dados são perdidos.

## Resource Limits

Cada instância possui limites de recursos configurados pelo tamanho escolhido:

- **CPU**: Limitado em nano-CPUs (1 core = 1.000.000.000 nanoCPUs)
- **Memory**: Limitado em bytes
- **Cost**: Calculado por hora com base nos recursos alocados

### Tamanhos Disponíveis

| Tamanho | CPU | RAM |
|---------|-----|-----|
| Nano1s  | 0.1 | 128MB |
| Nano2s  | 0.2 | 256MB |
| Nano4s  | 0.4 | 512MB |
| Micro1s | 0.5 | 1GB |
| Micro2s | 1.0 | 2GB |
| Small1s | 1.0 | 2GB |
| Small2s | 2.0 | 4GB |
| Medium1s| 2.0 | 4GB |
| Medium2s| 4.0 | 8GB |
| Large1s | 4.0 | 8GB |
| Large2s | 8.0 | 16GB |

## Segurança

O Cloudios implementa múltiplas camadas de segurança:

- **Isolamento por realm**: Multi-tenancy com isolamento lógico completo
- **Limites estritos de recursos**: CPU e RAM limitadas por instância
- **Redes isoladas**: Cada realm possui sua própria rede Docker
- **Volumes persistentes**: Dados isolados por instância
- **Autenticação JWT**: Acesso à API via tokens JWT
- **Authorization policies**: Controle de acesso por roles (RealmOwner, RealmAdmin, etc.)
