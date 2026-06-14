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
  const [realmFilter, setRealmFilter] = useState('');
  const [idFilter, setIdFilter] = useState('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  useEffect(() => {
    loadContainers();
  }, [search, realmFilter, idFilter]);

  const loadContainers = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getAllContainers(1, 100);
      
      let filtered = data.items || [];
      
      // Filter out system realm containers
      filtered = filtered.filter(c => c.realmName !== 'system');
      
      if (search) {
        filtered = filtered.filter(c => 
          c.name.toLowerCase().includes(search.toLowerCase())
        );
      }
      
      if (realmFilter) {
        filtered = filtered.filter(c => 
          c.realmName.toLowerCase().includes(realmFilter.toLowerCase())
        );
      }
      
      if (idFilter) {
        filtered = filtered.filter(c => 
          c.id.toLowerCase().includes(idFilter.toLowerCase())
        );
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
            placeholder="Search by name..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="search-input"
          />
          <input
            type="text"
            placeholder="Filter by realm..."
            value={realmFilter}
            onChange={(e) => setRealmFilter(e.target.value)}
            className="search-input"
          />
          <input
            type="text"
            placeholder="Filter by ID..."
            value={idFilter}
            onChange={(e) => setIdFilter(e.target.value)}
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
