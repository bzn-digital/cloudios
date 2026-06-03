export interface UserListResponse {
  items: UserItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface UserItem {
  id: string;
  email: string;
  role: string;
  isBlocked: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  role: string;
}

export interface UpdateUserRequest {
  role?: string;
  isBlocked?: boolean;
}
