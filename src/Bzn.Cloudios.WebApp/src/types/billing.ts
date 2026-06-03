export interface RealmBillingResponse {
  realmId: string;
  realmName: string;
  month: string;
  totalCostBRL: number;
  services: BillingServiceItem[];
}

export interface BillingServiceItem {
  containerId: string;
  containerName: string;
  costPerHourBRL: number;
  runningHours: number;
  totalCostBRL: number;
}

export interface GlobalBillingResponse {
  month: string;
  totalRevenueBRL: number;
  realms: RealmBillingItem[];
}

export interface RealmBillingItem {
  realmId: string;
  realmName: string;
  totalCostBRL: number;
  containerCount: number;
  activeContainerCount: number;
}

export interface ContainerMetricsResponse {
  containerId: string;
  from: string;
  to: string;
  dataPoints: MetricDataPoint[];
}

export interface MetricDataPoint {
  timestamp: string;
  cpuPercent: number;
  memoryUsedBytes: number;
  networkRxBytes: number;
  networkTxBytes: number;
}

export interface HostMetricsResponse {
  totalCpuPercent: number;
  totalMemoryUsedBytes: number;
  totalMemoryTotalBytes: number;
  activeContainers: number;
  diskUsedBytes: number;
  diskTotalBytes: number;
}
