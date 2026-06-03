import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { Modal } from '../components/Modal';
import { apiClient } from '../lib/api';
import { useToast } from '../contexts/ToastContext';
import type { ContainerListResponse, ContainerListItem } from '../types/container';
import type { CreateContainerRequest } from '../types/container';

export function Services() {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [containers, setContainers] = useState<ContainerListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [showNewServiceModal, setShowNewServiceModal] = useState(false);
  const [newServiceLoading, setNewServiceLoading] = useState(false);
  const [newServiceError, setNewServiceError] = useState<string | null>(null);
  const [networks, setNetworks] = useState<string[]>([]);
  const [networksLoading, setNetworksLoading] = useState(false);
  const [newServiceFormData, setNewServiceFormData] = useState<CreateContainerRequest>({
    name: '',
    imageName: '',
    internalPort: 80,
    hostPort: undefined,
    networkName: '',
    cpuLimitCores: 0.5,
    memoryLimitBytes: 256 * 1024 * 1024,
    costPerHourBRL: 0.02,
    environmentVariables: {},
  });
  const [envVars, setEnvVars] = useState<{ key: string; value: string }[]>([{ key: '', value: '' }]);

  useEffect(() => {
    loadContainers();
  }, [search, statusFilter]);

  const loadNetworks = async () => {
    try {
      setNetworksLoading(true);
      const data = await apiClient.getNetworks() as { networks: string[] };
      setNetworks(data.networks || []);
    } catch (err) {
      console.error('Failed to load networks:', err);
    } finally {
      setNetworksLoading(false);
    }
  };

  useEffect(() => {
    if (showNewServiceModal) {
      loadNetworks();
    }
  }, [showNewServiceModal]);

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

  const handleAction = async (id: string, action: () => Promise<unknown>, successMessage: string) => {
    try {
      setActionLoading(id);
      await action();
      await loadContainers();
      showToast('success', successMessage);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed');
      showToast('error', err instanceof Error ? err.message : 'Action failed');
    } finally {
      setActionLoading(null);
    }
  };

  const handleStart = (id: string) => {
    handleAction(id, () => apiClient.deployContainer(id), 'Service deployed successfully');
  };

  const handleStop = (id: string) => {
    handleAction(id, () => apiClient.stopContainer(id), 'Service stopped successfully');
  };

  const handleRestart = (id: string) => {
    handleAction(id, () => apiClient.restartContainer(id), 'Service restarted successfully');
  };

  const handleDelete = (id: string, name: string) => {
    const confirmed = window.prompt(`Type "${name}" to confirm deletion:`);
    if (confirmed === name) {
      handleAction(id, () => apiClient.deleteContainer(id), 'Service deleted successfully');
    }
  };

  const handleNewServiceSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setNewServiceLoading(true);
      setNewServiceError(null);

      if (!/^[a-z0-9-]+$/.test(newServiceFormData.name)) {
        setNewServiceError('Service name must contain only lowercase letters, numbers, and hyphens');
        return;
      }

      const envVarsObj: Record<string, string> = {};
      envVars.forEach((env) => {
        if (env.key && env.value) {
          envVarsObj[env.key] = env.value;
        }
      });

      const payload = {
        ...newServiceFormData,
        environmentVariables: envVarsObj,
      };

      await apiClient.createContainer(payload);
      setShowNewServiceModal(false);
      setNewServiceFormData({
        name: '',
        imageName: '',
        internalPort: 80,
        hostPort: undefined,
        networkName: '',
        cpuLimitCores: 0.5,
        memoryLimitBytes: 256 * 1024 * 1024,
        costPerHourBRL: 0.02,
        environmentVariables: {},
      });
      setEnvVars([{ key: '', value: '' }]);
      await loadContainers();
      showToast('success', 'Service created successfully');
    } catch (err) {
      setNewServiceError(err instanceof Error ? err.message : 'Failed to create service');
      showToast('error', err instanceof Error ? err.message : 'Failed to create service');
    } finally {
      setNewServiceLoading(false);
    }
  };

  const addEnvVar = () => {
    setEnvVars([...envVars, { key: '', value: '' }]);
  };

  const removeEnvVar = (index: number) => {
    setEnvVars(envVars.filter((_, i) => i !== index));
  };

  const updateEnvVar = (index: number, field: 'key' | 'value', value: string) => {
    const updated = [...envVars];
    updated[index][field] = value;
    setEnvVars(updated);
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
          <button className="btn btn-primary" onClick={() => setShowNewServiceModal(true)}>
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
          <p className="empty-state">No services deployed yet.</p>
        ) : (
          <div className="services-table">
            <table>
              <thead>
                <tr>
                  <th>Status</th>
                  <th>Name</th>
                  <th>Image</th>
                  <th>Public URL</th>
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
                    <td>
                      {container.publicUrl ? (
                        <a
                          href={container.publicUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="link-button"
                        >
                          {container.publicUrl}
                        </a>
                      ) : (
                        <span className="text-muted">Not configured</span>
                      )}
                    </td>
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

      <Modal
        isOpen={showNewServiceModal}
        onClose={() => setShowNewServiceModal(false)}
        title="New Service"
      >
        <form onSubmit={handleNewServiceSubmit} className="new-service-form">
          {newServiceError && <p className="error">{newServiceError}</p>}

          <div className="form-group">
            <label htmlFor="name">Service Name *</label>
            <input
              id="name"
              type="text"
              value={newServiceFormData.name}
              onChange={(e) => setNewServiceFormData({ ...newServiceFormData, name: e.target.value })}
              placeholder="my-service"
              required
              title="Only lowercase letters, numbers, and hyphens"
            />
            <small>Only lowercase letters, numbers, and hyphens (e.g., my-service)</small>
          </div>

          <div className="form-group">
            <label htmlFor="imageName">Docker Image *</label>
            <input
              id="imageName"
              type="text"
              value={newServiceFormData.imageName}
              onChange={(e) => setNewServiceFormData({ ...newServiceFormData, imageName: e.target.value })}
              placeholder="nginx:latest"
              required
            />
            <small>e.g., nginx:latest, n8nio/n8n, postgres:15</small>
          </div>

          <div className="form-group">
            <label htmlFor="internalPort">Internal Port *</label>
            <input
              id="internalPort"
              type="number"
              value={newServiceFormData.internalPort}
              onChange={(e) => setNewServiceFormData({ ...newServiceFormData, internalPort: parseInt(e.target.value) || 80 })}
              min={1}
              max={65535}
              required
            />
            <small>Port your application listens on (e.g., 80, 3000, 5678)</small>
          </div>

          <div className="form-group">
            <label htmlFor="hostPort">Host Port (Optional)</label>
            <input
              id="hostPort"
              type="number"
              value={newServiceFormData.hostPort || ''}
              onChange={(e) => setNewServiceFormData({ ...newServiceFormData, hostPort: e.target.value ? parseInt(e.target.value) : undefined })}
              min={1}
              max={65535}
            />
            <small>Port to expose on host (leave empty for random port)</small>
          </div>

          <div className="form-group">
            <label htmlFor="networkName">Network Name</label>
            <select
              id="networkName"
              value={newServiceFormData.networkName}
              onChange={(e) => setNewServiceFormData({ ...newServiceFormData, networkName: e.target.value })}
              disabled={networksLoading}
            >
              <option value="">Default realm network</option>
              {networks.map((network) => (
                <option key={network} value={network}>
                  {network}
                </option>
              ))}
            </select>
            <small>Select a network or leave empty for default realm network</small>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="cpuLimit">CPU Limit (vCPU) *</label>
              <input
                id="cpuLimit"
                type="number"
                step="0.1"
                min="0.1"
                max="4.0"
                value={newServiceFormData.cpuLimitCores}
                onChange={(e) => setNewServiceFormData({ ...newServiceFormData, cpuLimitCores: parseFloat(e.target.value) || 0.5 })}
                required
              />
              <small>0.1 to 4.0 vCPU</small>
            </div>

            <div className="form-group">
              <label htmlFor="memoryLimit">RAM Limit (MB) *</label>
              <input
                id="memoryLimit"
                type="number"
                min={128}
                max={8192}
                step={128}
                value={newServiceFormData.memoryLimitBytes / (1024 * 1024)}
                onChange={(e) => setNewServiceFormData({ ...newServiceFormData, memoryLimitBytes: (parseInt(e.target.value) || 256) * 1024 * 1024 })}
                required
              />
              <small>128MB to 8GB</small>
            </div>
          </div>

          <div className="form-group">
            <label>Environment Variables</label>
            {envVars.map((env, index) => (
              <div key={index} className="env-var-row">
                <input
                  type="text"
                  placeholder="KEY"
                  value={env.key}
                  onChange={(e) => updateEnvVar(index, 'key', e.target.value)}
                />
                <input
                  type="text"
                  placeholder="VALUE"
                  value={env.value}
                  onChange={(e) => updateEnvVar(index, 'value', e.target.value)}
                />
                {envVars.length > 1 && (
                  <button
                    type="button"
                    className="btn btn-sm btn-danger"
                    onClick={() => removeEnvVar(index)}
                  >
                    Remove
                  </button>
                )}
              </div>
            ))}
            <button type="button" className="btn btn-sm btn-secondary" onClick={addEnvVar}>
              + Add Variable
            </button>
          </div>

          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={newServiceLoading}>
              {newServiceLoading ? 'Creating...' : 'Create Service'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => setShowNewServiceModal(false)}>
              Cancel
            </button>
          </div>
        </form>
      </Modal>
    </Layout>
  );
}
