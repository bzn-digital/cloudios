export interface LoginRequest {
  email: string;
  password: string;
  realmName: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: UserInfo;
}

export interface UserInfo {
  id: string;
  email: string;
  role: string;
  realmId: string;
  realmName: string;
}
