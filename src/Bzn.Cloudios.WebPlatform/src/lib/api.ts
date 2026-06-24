import type { AdminContainerListResponse, ContainerActionResponse, ContainerDetailResponse, ContainerLogsResponse } from '../types/container';
import type { AdminManagedAppListResponse, ManagedAppActionResponse } from '../types/managedApp';
import type { RealmListResponse as RealmListResponseTyped, RealmStatsResponse, RealmDetail } from '../types/realm';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

class ApiClient {
  private baseUrl: string;
  private token: string | null = null;
  private onUnauthorized: (() => void) | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  setToken(token: string) {
    this.token = token;
  }

  clearToken() {
    this.token = null;
  }

  setUnauthorizedHandler(handler: () => void) {
    this.onUnauthorized = handler;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string> || {}),
    };

    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    const response = await fetch(url, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      this.clearToken();
      if (this.onUnauthorized) {
        this.onUnauthorized();
      }
      throw new Error('Unauthorized');
    }

    if (!response.ok) {
      const error = await response.text();
      throw new Error(`API Error: ${response.status} - ${error}`);
    }

    return response.json();
  }

  async get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' });
  }

  async post<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async put<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }

  // Admin endpoints
  async getAllContainers(page = 1, pageSize = 20): Promise<AdminContainerListResponse> {
    return this.get<AdminContainerListResponse>(`/containers/all?page=${page}&pageSize=${pageSize}`);
  }

  async getContainer(id: string): Promise<ContainerDetailResponse> {
    return this.get<ContainerDetailResponse>(`/containers/${id}`);
  }

  async getContainerLogs(id: string, tail = 100): Promise<ContainerLogsResponse> {
    return this.get<ContainerLogsResponse>(`/containers/${id}/logs?tail=${tail}`);
  }

  async getContainerMetrics(id: string): Promise<{ cpuPercent: number; memoryUsedBytes: number }> {
    return this.get<{ cpuPercent: number; memoryUsedBytes: number }>(`/containers/${id}/metrics`);
  }

  async startContainer(id: string): Promise<ContainerActionResponse> {
    return this.post<ContainerActionResponse>(`/containers/${id}/start`);
  }

  async restartContainer(id: string): Promise<ContainerActionResponse> {
    return this.post<ContainerActionResponse>(`/containers/${id}/restart`);
  }

  async stopContainer(id: string): Promise<ContainerActionResponse> {
    return this.post<ContainerActionResponse>(`/containers/${id}/stop`);
  }

  async deleteContainer(id: string): Promise<void> {
    return this.delete<void>(`/containers/${id}`);
  }

  // Admin Managed Apps endpoints
  async getAdminManagedApps(realmId?: string, status?: string, page = 1, pageSize = 20): Promise<AdminManagedAppListResponse> {
    const params = new URLSearchParams();
    if (realmId) params.append('realmId', realmId);
    if (status) params.append('status', status);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    return this.get<AdminManagedAppListResponse>(`/managed-apps/all?${params.toString()}`);
  }

  async getRealms(page = 1, pageSize = 20, search?: string, status?: string, sortBy?: string): Promise<RealmListResponseTyped> {
    const params = new URLSearchParams();
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    if (search) params.append('search', search);
    if (status) params.append('status', status);
    if (sortBy) params.append('sortBy', sortBy);
    return this.get<RealmListResponseTyped>(`/realms?${params.toString()}`);
  }

  async getRealmStats(id: string): Promise<RealmStatsResponse> {
    return this.get<RealmStatsResponse>(`/realms/${id}/stats`);
  }

  async getRealmDetail(id: string): Promise<RealmDetail> {
    return this.get<RealmDetail>(`/realms/${id}`);
  }

  async updateRealm(id: string, data: { name?: string; isActive?: boolean; quotas?: Record<string, number> }): Promise<void> {
    return this.put<void>(`/realms/${id}`, data);
  }

  async createRealm(data: { name: string; slug: string; ownerEmail: string; ownerPassword: string }): Promise<void> {
    return this.post<void>('/realms', data);
  }

  async updateQuotas(id: string, data: { maxContainers?: number; maxDatabases?: number; maxManagedApps?: number; maxRamBytes?: number; maxCpuCores?: number }): Promise<void> {
    return this.put<void>(`/realms/${id}/quotas`, data);
  }

  async suspendRealm(id: string): Promise<void> {
    return this.post<void>(`/realms/${id}/suspend`);
  }

  async reactivateRealm(id: string): Promise<void> {
    return this.post<void>(`/realms/${id}/reactivate`);
  }

  async restartManagedApp(id: string): Promise<ManagedAppActionResponse> {
    return this.post<ManagedAppActionResponse>(`/managed-apps/${id}/restart`);
  }

  async stopManagedApp(id: string): Promise<ManagedAppActionResponse> {
    return this.post<ManagedAppActionResponse>(`/managed-apps/${id}/stop`);
  }

  async deleteManagedApp(id: string): Promise<void> {
    return this.delete<void>(`/managed-apps/${id}`);
  }
}

export const apiClient = new ApiClient(API_BASE_URL);
