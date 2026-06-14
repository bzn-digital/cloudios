import type { AdminContainerListResponse, ContainerActionResponse } from '../types/container';

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

  async restartContainer(id: string): Promise<ContainerActionResponse> {
    return this.post<ContainerActionResponse>(`/containers/${id}/restart`);
  }

  async stopContainer(id: string): Promise<ContainerActionResponse> {
    return this.post<ContainerActionResponse>(`/containers/${id}/stop`);
  }

  async deleteContainer(id: string): Promise<void> {
    return this.delete<void>(`/containers/${id}`);
  }
}

export const apiClient = new ApiClient(API_BASE_URL);
