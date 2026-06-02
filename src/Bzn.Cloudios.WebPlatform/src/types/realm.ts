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
  isActive: boolean;
  createdAt: string;
  userCount: number;
  containerCount: number;
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
