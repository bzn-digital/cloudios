import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { AdminContainerListItem } from '../types/container';

export function Services() {
  const navigate = useNavigate();
  const [containers, setContainers] = useState<AdminContainerListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  // Debounce search to prevent re-renders on every keystroke
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
    }, 300);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    loadContainers();
  }, [debouncedSearch]);

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
      
      return { type: 'advanced', filters };
    }
    
    // Check if query contains single key=value
    if (query.includes('=')) {
      const match = query.match(/^(\w+)=(.+)$/);
      if (match) {
        const [, field, value] = match;
        const cleanValue = value.replace(/^["']|["']$/g, '');
        return { type: 'advanced', filters: [{ field, value: cleanValue }] };
      }
    }
    
    // Default to literal search
    return { type: 'literal', query };
  };

  const loadContainers = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getAllContainers(1, 100);
      
      let filtered = data.items || [];
      
      // Filter out system realm containers
      filtered = filtered.filter(c => c.realmName !== 'system');
      
      if (debouncedSearch) {
        const parsed = parseSearchQuery(debouncedSearch);
        
        if (parsed.type === 'advanced') {
          // Apply advanced filters
          filtered = filtered.filter(c => {
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
        } else {
          // Literal search across all fields
          const searchLower = parsed.query.toLowerCase();
          filtered = filtered.filter(c => 
            c.name.toLowerCase().includes(searchLower) ||
            c.realmName.toLowerCase().includes(searchLower) ||
            c.id.toLowerCase().includes(searchLower) ||
            c.imageName.toLowerCase().includes(searchLower) ||
            c.status.toLowerCase().includes(searchLower)
          );
        }
      }
      
      setContainers(filtered);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load containers');
    } finally {
      setLoading(false);
    }
  };

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

  if (loading && containers.length === 0) {
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
              {containers.length === 0 ? (
                <tr>
                  <td colSpan={6}>
                    <p className="empty-state">No services found.</p>
                  </td>
                </tr>
              ) : (
                containers.map((container) => (
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
