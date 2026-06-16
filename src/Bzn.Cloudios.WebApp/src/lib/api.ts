const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost';

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
      const errorText = await response.text();
      try {
        const errorJson = JSON.parse(errorText);
        throw new Error(errorJson.detail || errorText);
      } catch {
        throw new Error(`API Error: ${response.status} - ${errorText}`);
      }
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

  // Billing endpoints
  async getRealmBilling(year: number, month: number) {
    return this.get(`/billing/realm?year=${year}&month=${month}`);
  }

  // Metrics endpoints
  async getRealmMetricsHistory(from: string, to: string) {
    return this.get(`/metrics/realm/history?from=${from}&to=${to}`);
  }

  // Container endpoints
  async getContainers(search?: string, status?: string, page = 1, pageSize = 20) {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (search) params.append('search', search);
    if (status) params.append('status', status);
    return this.get(`/containers?${params.toString()}`);
  }

  async getContainer(id: string) {
    return this.get(`/containers/${id}`);
  }

  async createContainer(data: unknown) {
    return this.post('/containers', data);
  }

  async deployContainer(id: string) {
    return this.post(`/containers/${id}/deploy`);
  }

  async startContainer(id: string) {
    return this.post(`/containers/${id}/start`);
  }

  async stopContainer(id: string) {
    return this.post(`/containers/${id}/stop`);
  }

  async restartContainer(id: string) {
    return this.post(`/containers/${id}/restart`);
  }

  async deleteContainer(id: string) {
    return this.delete(`/containers/${id}`);
  }

  async getContainerLogs(id: string, tail = 100) {
    return this.get(`/containers/${id}/logs?tail=${tail}`);
  }

  async getNetworks() {
    return this.get('/networks');
  }

  // Registration endpoint
  async register(data: { realmName: string; email: string; password: string }) {
    return this.post('/register', data);
  }
}

export const apiClient = new ApiClient(API_BASE_URL);
