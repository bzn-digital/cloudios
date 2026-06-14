import { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { AdminContainerListItem } from '../types/container';

export function Services() {
  const navigate = useNavigate();
  const [allContainers, setAllContainers] = useState<AdminContainerListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  useEffect(() => {
    loadContainers();
  }, []);

  const parseSearchQuery = (query: string) => {
    // Check if query contains key=value syntax with &&
    if (query.includes('=') && query.includes('&&')) {
      const conditions = query.split('&&').map(c => c.trim());
      const filters: { field: string; value: string }[] = [];

      for (const condition of conditions) {
        const match = condition.match(/^(\w+)=(.+)$/);
        if (match) {
          const [, field, value] = match;
          // Remove quotes if present
          const cleanValue = value.replace(/^["']|["']$/g, '');
          filters.push({ field, value: cleanValue });
        }
      }

      return { type: 'advanced' as const, filters };
    }

    // Check if query contains single key=value
    if (query.includes('=')) {
      const match = query.match(/^(\w+)=(.+)$/);
      if (match) {
        const [, field, value] = match;
        const cleanValue = value.replace(/^["']|["']$/g, '');
        return { type: 'advanced' as const, filters: [{ field, value: cleanValue }] };
      }
    }

    // Default to literal search
    return { type: 'literal' as const, query };
  };

  const loadContainers = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getAllContainers(1, 100);
      
      // Filter out system realm containers
      const filtered = (data.items || []).filter(c => c.realmName !== 'system');
      
      setAllContainers(filtered);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load containers');
    } finally {
      setLoading(false);
    }
  };

  // Filter containers based on search query using useMemo to prevent re-renders
  const filteredContainers = useMemo(() => {
    if (!search) return allContainers;

    const parsed = parseSearchQuery(search);

    if (parsed.type === 'advanced' && parsed.filters) {
      // Apply advanced filters
      return allContainers.filter(c => {
        return parsed.filters.every(filter => {
          const value = filter.value.toLowerCase();
          switch (filter.field) {
            case 'realm':
            case 'realmName':
              return c.realmName.toLowerCase().includes(value);
            case 'name':
            case 'serviceName':
              return c.name.toLowerCase().includes(value);
            case 'id':
              return c.id.toLowerCase().includes(value);
            case 'image':
            case 'imageName':
              return c.imageName.toLowerCase().includes(value);
            case 'status':
              return c.status.toLowerCase().includes(value);
            default:
              return false;
          }
        });
      });
    } else if (parsed.type === 'literal' && parsed.query) {
      // Literal search across all fields
      const searchLower = parsed.query.toLowerCase();
      return allContainers.filter(c =>
        c.name.toLowerCase().includes(searchLower) ||
        c.realmName.toLowerCase().includes(searchLower) ||
        c.id.toLowerCase().includes(searchLower) ||
        c.imageName.toLowerCase().includes(searchLower) ||
        c.status.toLowerCase().includes(searchLower)
      );
    }

    return allContainers;
  }, [allContainers, search]);

  const handleAction = async (id: string, action: () => Promise<unknown>, successMessage: string) => {
    try {
      setActionLoading(id);
      await action();
      await loadContainers();
      alert(successMessage);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed');
      alert(err instanceof Error ? err.message : 'Action failed');
    } finally {
      setActionLoading(null);
    }
  };

  const handleRestart = (id: string) => {
    handleAction(id, () => apiClient.restartContainer(id), 'Service restarted successfully');
  };

  const handleStop = (id: string) => {
    handleAction(id, () => apiClient.stopContainer(id), 'Service stopped successfully');
  };

  const handleDelete = (id: string, name: string) => {
    const confirmed = window.prompt(`Type "${name}" to confirm deletion:`);
    if (confirmed === name) {
      handleAction(id, () => apiClient.deleteContainer(id), 'Service deleted successfully');
    }
  };

  if (loading && allContainers.length === 0) {
    return (
      <Layout>
        <div className="services">
          <h1>Services</h1>
          <p>Loading...</p>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Services</h1>
        </div>

        <div className="services-filters">
          <input
            type="text"
            placeholder="Search (e.g., 'nginx', realm='bznteste' && name='nginx')"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="search-input"
          />
        </div>

        {error && <p className="error">{error}</p>}

        <div className="services-table">
          <table>
            <thead>
              <tr>
                <th>Realm</th>
                <th>Service Name</th>
                <th>Network</th>
                <th>Image</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredContainers.length === 0 ? (
                <tr>
                  <td colSpan={6}>
                    <p className="empty-state">No services found.</p>
                  </td>
                </tr>
              ) : (
                filteredContainers.map((container) => (
                  <tr key={container.id}>
                    <td>
                      <span className="realm-badge">{container.realmName}</span>
                    </td>
                    <td>
                      <button
                        className="link-button"
                        onClick={() => navigate(`/services/${container.id}`)}
                      >
                        {container.name}
                      </button>
                    </td>
                    <td>
                      <span className="network-badge">
                        {container.networkName || 'default'}
                      </span>
                    </td>
                    <td>{container.imageName}</td>
                    <td>
                      <span className={`status-badge status-${container.status.toLowerCase()}`}>
                        {container.status}
                      </span>
                    </td>
                    <td>
                      <div className="action-buttons">
                        {container.status === 'Running' ? (
                          <>
                            <button
                              className="btn btn-sm btn-warning"
                              onClick={() => handleRestart(container.id)}
                              disabled={actionLoading === container.id}
                            >
                              Restart
                            </button>
                            <button
                              className="btn btn-sm btn-danger"
                              onClick={() => handleStop(container.id)}
                              disabled={actionLoading === container.id}
                            >
                              Stop
                            </button>
                          </>
                        ) : null}
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => handleDelete(container.id, container.name)}
                          disabled={actionLoading === container.id}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </Layout>
  );
}
