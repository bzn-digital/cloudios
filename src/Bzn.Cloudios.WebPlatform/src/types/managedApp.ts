export type ManagedAppStatus = 'Imaging' | 'Initializing' | 'Running' | 'Stopped' | 'Failed' | 'Terminated';

export interface AdminManagedAppListItem {
  id: string;
  realmId: string;
  realmName: string;
  templateId: string;
  templateName: string;
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

export interface AdminManagedAppListResponse {
  items: AdminManagedAppListItem[];
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

export interface Realm {
  id: string;
  name: string;
}
