import { useState, useEffect } from 'react';
import { Layout } from '../components/Layout';
import { CreateManagedAppModal } from '../components/CreateManagedAppModal';
import { apiClient } from '../lib/api';
import { useToast } from '../contexts/ToastContext';
import type { ManagedAppInstanceListItem, ManagedAppInstanceListResponse, ManagedAppTemplate } from '../types/managedApp';

const ManagedApps = () => {
  const { showToast } = useToast();
  const [items, setItems] = useState<ManagedAppInstanceListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  const loadInstances = async () => {
    try {
      setLoading(true);
      setError(null);
      const status = statusFilter === 'All' ? undefined : statusFilter;
      const data = await apiClient.getManagedAppInstances(search, status, 1, 100) as ManagedAppInstanceListResponse;
      setItems(data.items || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load managed apps');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadInstances();
  }, [search, statusFilter]);

  // Poll instances in Imaging or Initializing status
  useEffect(() => {
    const transitioningInstances = items.filter(i => i.status === 'Imaging' || i.status === 'Initializing');

    if (transitioningInstances.length === 0) return;

    const interval = setInterval(async () => {
      try {
        const status = statusFilter === 'All' ? undefined : statusFilter;
        const data = await apiClient.getManagedAppInstances(search, status, 1, 100) as ManagedAppInstanceListResponse;
        setItems(data.items || []);
      } catch (err) {
        console.error('Failed to poll managed app status:', err);
      }
    }, 5000); // Poll every 5 seconds

    return () => clearInterval(interval);
  }, [items, search, statusFilter]);

  const handleAction = async (id: string, action: () => Promise<unknown>, successMessage: string) => {
    try {
      setActionLoading(id);
      await action();
      await loadInstances();
      showToast('success', successMessage);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed');
      showToast('error', err instanceof Error ? err.message : 'Action failed');
    } finally {
      setActionLoading(null);
    }
  };

  const handleStart = (id: string) => {
    handleAction(id, () => apiClient.startManagedApp(id), 'Managed app started successfully');
  };

  const handleRestart = (id: string) => {
    handleAction(id, () => apiClient.restartManagedApp(id), 'Managed app restarted successfully');
  };

  const handleStop = (id: string) => {
    handleAction(id, () => apiClient.stopManagedApp(id), 'Managed app stopped successfully');
  };

  const handleDelete = (id: string, name: string) => {
    const confirmed = window.prompt(`Type "${name}" to confirm deletion:`);
    if (confirmed === name) {
      handleAction(id, () => apiClient.deleteManagedApp(id), 'Managed app deleted successfully');
    }
  };

  const handleTemplateSelected = (template: ManagedAppTemplate) => {
    // Step 2 will be implemented in a future issue
    console.log('Selected template:', template);
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

  const getInstanceSizeSpecs = (size: string) => {
    // Map instance size names to specs (should match backend InstanceSizeCatalog)
    const sizeMap: Record<string, { cpu: number; memory: number }> = {
      'Nano1s': { cpu: 0.25, memory: 256 * 1024 * 1024 },
      'Micro1s': { cpu: 0.5, memory: 512 * 1024 * 1024 },
      'Small1s': { cpu: 1.0, memory: 1024 * 1024 * 1024 },
      'Medium1s': { cpu: 2.0, memory: 2 * 1024 * 1024 * 1024 },
      'Large1s': { cpu: 4.0, memory: 4 * 1024 * 1024 * 1024 },
    };
    return sizeMap[size] || { cpu: 0, memory: 0 };
  };

  if (loading && items.length === 0) {
    return (
      <Layout>
        <div className="services">
          <h1>Managed Apps</h1>
          <p>Loading...</p>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Managed Apps</h1>
          <button className="btn btn-primary" onClick={() => setIsCreateModalOpen(true)}>
            + Create App
          </button>
        </div>

        <div className="services-filters">
          <input
            type="text"
            placeholder="Search apps..."
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
            <option value="Imaging">Imaging</option>
            <option value="Initializing">Initializing</option>
          </select>
        </div>

        {error && <p className="error">{error}</p>}

        <div className="services-table">
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Name</th>
                <th>App</th>
                <th>Instance Size</th>
                <th>Internal Access</th>
                <th>Cost/Month</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td colSpan={8}>
                    <p className="empty-state">No managed apps yet. Click Create App to get started.</p>
                  </td>
                </tr>
              ) : (
                items.map((item) => {
                  const sizeSpecs = getInstanceSizeSpecs(item.size);
                  return (
                    <tr key={item.id}>
                      <td>
                        <span className={`status-badge status-${item.status.toLowerCase()}`}>
                          {item.status === 'Imaging' ? (
                            <>
                              <span className="loading-spinner">⟳</span>
                              Imaging
                            </>
                          ) : item.status === 'Initializing' ? (
                            <>
                              <span className="loading-spinner">⟳</span>
                              Initializing
                            </>
                          ) : (
                            item.status
                          )}
                        </span>
                      </td>
                      <td>{item.name}</td>
                      <td>{item.templateDisplayName}</td>
                      <td>
                        {formatCpu(sizeSpecs.cpu)} / {formatBytes(sizeSpecs.memory)}
                      </td>
                      <td>
                        <code className="internal-connection" title="Services on the same network can connect using this internal address">
                          {item.internalAccess}
                        </code>
                      </td>
                      <td>R$ {item.currentMonthCostBRL.toFixed(2)}</td>
                      <td>{new Date(item.createdAt).toLocaleDateString()}</td>
                      <td>
                        <div className="action-buttons">
                          {item.status === 'Stopped' || item.status === 'Failed' ? (
                            <button
                              className="btn btn-sm btn-success"
                              onClick={() => handleStart(item.id)}
                              disabled={actionLoading === item.id}
                              title="Start"
                            >
                              ▶
                            </button>
                          ) : null}
                          {item.status === 'Running' ? (
                            <>
                              <button
                                className="btn btn-sm btn-warning"
                                onClick={() => handleRestart(item.id)}
                                disabled={actionLoading === item.id}
                                title="Restart"
                              >
                                ⟳
                              </button>
                              <button
                                className="btn btn-sm btn-danger"
                                onClick={() => handleStop(item.id)}
                                disabled={actionLoading === item.id}
                                title="Stop"
                              >
                                ⏹
                              </button>
                            </>
                          ) : null}
                          <button
                            className="btn btn-sm btn-danger"
                            onClick={() => handleDelete(item.id, item.name)}
                            disabled={actionLoading === item.id}
                            title="Delete"
                          >
                            🗑
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      <CreateManagedAppModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onTemplateSelected={handleTemplateSelected}
      />
    </Layout>
  );
};

export default ManagedApps;
