export interface ContainerListResponse {
  items: ContainerListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface ContainerListItem {
  id: string;
  name: string;
  imageName: string;
  internalPort: number;
  status: string;
  cpuLimitCores: number;
  memoryLimitBytes: number;
  costPerHourBRL: number;
  currentMonthCostBRL: number;
  publicUrl?: string;
  startedAtUtc?: string;
  createdAt: string;
  networkName?: string;
}

export interface ContainerDetailResponse {
  id: string;
  name: string;
  imageName: string;
  internalPort: number;
  status: string;
  cpuLimitCores: number;
  memoryLimitBytes: number;
  costPerHourBRL: number;
  currentMonthCostBRL: number;
  dockerContainerId?: string;
  startedAtUtc?: string;
  createdAt: string;
  volumes: ContainerVolumeDto[];
  environmentVariables: object[];
}

export interface CreateContainerRequest {
  name: string;
  imageName: string;
  internalPort: number;
  hostPort?: number;
  networkName: string;
  cpuLimitCores: number;
  memoryLimitBytes: number;
  costPerHourBRL: number;
  environmentVariables: Record<string, string>;
}

export interface ContainerActionResponse {
  id: string;
  status: string;
  dockerContainerId?: string;
  startedAtUtc?: string;
}

export interface ContainerVolumeDto {
  id: string;
  hostPath: string;
  containerPath: string;
  isReadOnly: boolean;
}

export interface ContainerVolumeRequest {
  hostPath: string;
  containerPath: string;
  isReadOnly: boolean;
}

export interface ContainerEnvVarDto {
  id: string;
  key: string;
  value: string;
}

export interface ContainerEnvVarSecureDto {
  id: string;
  key: string;
  value: string;
}

export interface ContainerLogsResponse {
  containerId: string;
  logs: ContainerLogEntry[];
}

export interface ContainerLogEntry {
  timestamp: string;
  stream: string;
  line: string;
}

export interface AdminContainerListResponse {
  items: AdminContainerListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface AdminContainerListItem {
  id: string;
  realmId: string;
  realmName: string;
  name: string;
  imageName: string;
  status: string;
  cpuLimitCores: number;
  memoryLimitBytes: number;
  costPerHourBRL: number;
  currentMonthCostBRL: number;
}
