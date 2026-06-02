import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { ContainerListResponse, ContainerListItem } from '../types/container';

export function Services() {
  const navigate = useNavigate();
  const [containers, setContainers] = useState<ContainerListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  useEffect(() => {
    loadContainers();
  }, [search, statusFilter]);

  const loadContainers = async () => {
    try {
      setLoading(true);
      const status = statusFilter === 'All' ? undefined : statusFilter;
      const data = await apiClient.getContainers(search, status, 1, 100) as ContainerListResponse;
      setContainers(data.items || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load containers');
    } finally {
      setLoading(false);
    }
  };

  const handleAction = async (id: string, action: () => Promise<unknown>) => {
    try {
      setActionLoading(id);
      await action();
      await loadContainers();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed');
    } finally {
      setActionLoading(null);
    }
  };

  const handleStart = (id: string) => {
    handleAction(id, () => apiClient.startContainer(id));
  };

  const handleStop = (id: string) => {
    handleAction(id, () => apiClient.stopContainer(id));
  };

  const handleRestart = (id: string) => {
    handleAction(id, () => apiClient.restartContainer(id));
  };

  const handleDelete = (id: string, name: string) => {
    const confirmed = window.prompt(`Type "${name}" to confirm deletion:`);
    if (confirmed === name) {
      handleAction(id, () => apiClient.deleteContainer(id));
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)} KB`;
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
  };

  const formatCpu = (cores: number) => {
    if (cores < 1) return `${(cores * 1000).toFixed(0)} m`;
    return `${cores.toFixed(2)} vCPU`;
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
          <button className="btn btn-primary" onClick={() => navigate('/services/new')}>
            + New Service
          </button>
        </div>

        <div className="services-filters">
          <input
            type="text"
            placeholder="Search services..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="search-input"
          />
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="status-filter"
          >
            <option value="All">All Status</option>
            <option value="Running">Running</option>
            <option value="Stopped">Stopped</option>
            <option value="Failed">Failed</option>
          </select>
        </div>

        {error && <p className="error">{error}</p>}

        {containers.length === 0 ? (
          <p className="empty-state">No services found.</p>
        ) : (
          <div className="services-table">
            <table>
              <thead>
                <tr>
                  <th>Status</th>
                  <th>Name</th>
                  <th>Image</th>
                  <th>CPU Limit</th>
                  <th>RAM Limit</th>
                  <th>Cost (Month)</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {containers.map((container) => (
                  <tr key={container.id}>
                    <td>
                      <span className={`status-badge status-${container.status.toLowerCase()}`}>
                        {container.status}
                      </span>
                    </td>
                    <td>
                      <button
                        className="link-button"
                        onClick={() => navigate(`/services/${container.id}`)}
                      >
                        {container.name}
                      </button>
                    </td>
                    <td>{container.imageName}</td>
                    <td>{formatCpu(container.cpuLimitCores)}</td>
                    <td>{formatBytes(container.memoryLimitBytes)}</td>
                    <td>R$ {container.currentMonthCostBRL.toFixed(2)}</td>
                    <td>
                      <div className="action-buttons">
                        {container.status === 'Stopped' || container.status === 'Failed' ? (
                          <button
                            className="btn btn-sm btn-success"
                            onClick={() => handleStart(container.id)}
                            disabled={actionLoading === container.id}
                          >
                            Start
                          </button>
                        ) : null}
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
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Layout>
  );
}
