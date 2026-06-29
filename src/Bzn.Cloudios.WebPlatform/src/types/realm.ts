export interface RealmListResponse {
  items: RealmItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface RealmItem {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAt: string;
  userCount: number;
  containerCount: number;
  monthlyCostBRL: number;
}

export interface RealmDetailResponse {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  users: RealmUserItem[];
}

export interface RealmUserItem {
  id: string;
  email: string;
  role: string;
  isBlocked: boolean;
}

export interface CreateRealmRequest {
  name: string;
}

export interface UpdateRealmRequest {
  name: string;
  isActive: boolean;
}

export interface RealmStatsResponse {
  usersCount: number;
  containersCount: number;
  databasesCount: number;
  managedAppsCount: number;
  monthlyCostBRL: number;
  quotas: RealmQuotas;
  usage: RealmUsage;
}

export interface RealmQuotas {
  maxContainers?: number;
  maxDatabases?: number;
  maxManagedApps?: number;
  maxRamBytes?: number;
  maxCpuCores?: number;
}

export interface RealmUsage {
  containersCount: number;
  databasesCount: number;
  managedAppsCount: number;
  ramBytesUsed: number;
  cpuCoresUsed: number;
}

export interface RealmResource {
  id: string;
  name: string;
  type: 'container' | 'database';
  status: string;
  costBRL: number;
}

export interface RealmUser {
  id: string;
  email: string;
  role: string;
  isBlocked: boolean;
  createdAt: string;
}

export interface BillingHistoryItem {
  month: string;
  costBRL: number;
}

export interface RealmDetail {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAt: string;
  ownerEmail?: string;
  resources?: RealmResource[];
  users?: RealmUser[];
  billingHistory?: BillingHistoryItem[];
  quotas?: RealmQuotas;
}
