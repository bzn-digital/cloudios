import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { apiClient } from '../../lib/api';

describe('ApiClient', () => {
  let mockFetch: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    mockFetch = vi.fn();
    global.fetch = mockFetch;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should make GET request with correct URL', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ data: 'test' }),
    });

    await apiClient.get('/test');

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/test'),
      expect.objectContaining({
        method: 'GET',
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
        }),
      })
    );
  });

  it('should make POST request with data', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ success: true }),
    });

    const data = { name: 'test' };
    await apiClient.post('/test', data);

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/test'),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(data),
      })
    );
  });

  it('should include Authorization header when token is set', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ data: 'test' }),
    });

    apiClient.setToken('test-token');
    await apiClient.get('/test');

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/test'),
      expect.objectContaining({
        headers: expect.objectContaining({
          'Authorization': 'Bearer test-token',
        }),
      })
    );
  });

  it('should clear token and call unauthorized handler on 401', async () => {
    const unauthorizedHandler = vi.fn();
    mockFetch.mockResolvedValue({
      status: 401,
      ok: false,
      text: async () => 'Unauthorized',
    });

    apiClient.setToken('test-token');
    apiClient.setUnauthorizedHandler(unauthorizedHandler);

    await expect(apiClient.get('/test')).rejects.toThrow('Unauthorized');

    expect(unauthorizedHandler).toHaveBeenCalled();
  });

  it('should throw error on non-OK response', async () => {
    mockFetch.mockResolvedValue({
      status: 500,
      ok: false,
      text: async () => 'Internal Server Error',
    });

    await expect(apiClient.get('/test')).rejects.toThrow('API Error: 500 - Internal Server Error');
  });

  it('should make DELETE request', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ success: true }),
    });

    await apiClient.delete('/test');

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/test'),
      expect.objectContaining({
        method: 'DELETE',
      })
    );
  });

  it('should make PUT request with data', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ success: true }),
    });

    const data = { name: 'updated' };
    await apiClient.put('/test', data);

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/test'),
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(data),
      })
    );
  });
});
