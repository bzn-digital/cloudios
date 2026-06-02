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
