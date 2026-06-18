export type ManagedAppStatus = 'Imaging' | 'Initializing' | 'Running' | 'Stopped' | 'Failed' | 'Terminated';

export interface ManagedAppInstanceListItem {
  id: string;
  realmId: string;
  templateId: string;
  templateDisplayName: string;
  name: string;
  status: ManagedAppStatus;
  size: string;
  hostPort: number;
  internalPort: number;
  internalAccess: string; // "{name}:{internalPort}"
  dockerContainerId: string | null;
  cpuLimitCores: number;
  memoryLimitBytes: number;
  costPerHourBRL: number;
  currentMonthCostBRL: number;
  createdAt: string;
  startedAtUtc: string | null;
  stoppedAtUtc: string | null;
}

export interface ManagedAppInstanceListResponse {
  items: ManagedAppInstanceListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface ManagedAppActionResponse {
  id: string;
  status: string;
  dockerContainerId: string | null;
  startedAtUtc: string | null;
}

export interface ManagedAppTemplate {
  id: string;
  slug: string;
  displayName: string;
  name: string;
  description: string;
  category: string;
  iconUrl: string;
  internalPort: number;
  defaultInstanceSize: string;
}

export interface ManagedAppTemplateListResponse {
  items: ManagedAppTemplate[];
  categories: string[];
}

export interface CreateManagedAppRequest {
  templateId: string;
  name: string;
  instanceSize: string;
}

export interface ManagedAppInstanceDetailResponse {
  id: string;
  realmId: string;
  templateId: string;
  templateDisplayName: string;
  name: string;
  status: ManagedAppStatus;
  size: string;
  hostPort: number;
  internalPort: number;
  internalAccess: string;
  dockerContainerId: string | null;
  cpuLimitCores: number;
  memoryLimitBytes: number;
  costPerHourBRL: number;
  currentMonthCostBRL: number;
  createdAt: string;
  startedAtUtc: string | null;
  stoppedAtUtc: string | null;
}
