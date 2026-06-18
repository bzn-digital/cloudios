import { useState, useEffect } from 'react';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { AdminManagedAppListItem, Realm } from '../types/managedApp';

export function ManagedApps() {
  const [allApps, setAllApps] = useState<AdminManagedAppListItem[]>([]);
  const [realms, setRealms] = useState<Realm[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [realmFilter, setRealmFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  useEffect(() => {
    loadRealms();
    loadApps();
  }, [realmFilter, statusFilter, page]);

  const loadRealms = async () => {
    try {
      const data = await apiClient.getRealms();
      setRealms(data.items || []);
    } catch (err) {
      console.error('Failed to load realms:', err);
    }
  };

  const loadApps = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiClient.getAdminManagedApps(realmFilter, statusFilter, page, pageSize);
      setAllApps(data.items || []);
      setTotalCount(data.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load managed apps');
    } finally {
      setLoading(false);
    }
  };

  const handleAction = async (id: string, action: () => Promise<unknown>, successMessage: string) => {
    try {
      setActionLoading(id);
      await action();
      await loadApps();
      alert(successMessage);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed');
      alert(err instanceof Error ? err.message : 'Action failed');
    } finally {
      setActionLoading(null);
    }
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
    const sizeMap: Record<string, { cpu: number; memory: number }> = {
      'Nano1s': { cpu: 0.25, memory: 256 * 1024 * 1024 },
      'Micro1s': { cpu: 0.5, memory: 512 * 1024 * 1024 },
      'Small1s': { cpu: 1.0, memory: 1024 * 1024 * 1024 },
      'Medium1s': { cpu: 2.0, memory: 2 * 1024 * 1024 * 1024 },
      'Large1s': { cpu: 4.0, memory: 4 * 1024 * 1024 * 1024 },
    };
    return sizeMap[size] || { cpu: 0, memory: 0 };
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  if (loading && allApps.length === 0) {
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
        </div>

        <div className="services-filters">
          <select
            value={realmFilter}
            onChange={(e) => {
              setRealmFilter(e.target.value);
              setPage(1);
            }}
            className="status-filter"
          >
            <option value="">All Realms</option>
            {realms.map((realm) => (
              <option key={realm.id} value={realm.id}>
                {realm.name}
              </option>
            ))}
          </select>
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPage(1);
            }}
            className="status-filter"
          >
            <option value="">All Status</option>
            <option value="Running">Running</option>
            <option value="Stopped">Stopped</option>
            <option value="Failed">Failed</option>
            <option value="Imaging">Imaging</option>
            <option value="Initializing">Initializing</option>
            <option value="Terminated">Terminated</option>
          </select>
        </div>

        {error && <p className="error">{error}</p>}

        <div className="services-table">
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Realm</th>
                <th>App</th>
                <th>Name</th>
                <th>Instance Size</th>
                <th>Internal Access</th>
                <th>Host Port</th>
                <th>Cost/Month</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {allApps.length === 0 ? (
                <tr>
                  <td colSpan={10}>
                    <p className="empty-state">No managed apps found.</p>
                  </td>
                </tr>
              ) : (
                allApps.map((app) => {
                  const sizeSpecs = getInstanceSizeSpecs(app.size);
                  return (
                    <tr key={app.id}>
                      <td>
                        <span className={`status-badge status-${app.status.toLowerCase()}`}>
                          {app.status === 'Imaging' ? (
                            <>
                              <span className="loading-spinner">⟳</span>
                              Imaging
                            </>
                          ) : app.status === 'Initializing' ? (
                            <>
                              <span className="loading-spinner">⟳</span>
                              Initializing
                            </>
                          ) : (
                            app.status
                          )}
                        </span>
                      </td>
                      <td>
                        <span className="realm-badge">{app.realmName}</span>
                      </td>
                      <td>{app.templateDisplayName}</td>
                      <td>{app.name}</td>
                      <td>
                        {formatCpu(sizeSpecs.cpu)} / {formatBytes(sizeSpecs.memory)}
                      </td>
                      <td>
                        <code className="internal-connection" title="Services on the same network can connect using this internal address">
                          {app.internalAccess}
                        </code>
                      </td>
                      <td>{app.hostPort}</td>
                      <td>R$ {app.currentMonthCostBRL.toFixed(2)}</td>
                      <td>{new Date(app.createdAt).toLocaleDateString()}</td>
                      <td>
                        <div className="action-buttons">
                          {app.status === 'Running' ? (
                            <>
                              <button
                                className="btn btn-sm btn-warning"
                                onClick={() => handleRestart(app.id)}
                                disabled={actionLoading === app.id}
                              >
                                Restart
                              </button>
                              <button
                                className="btn btn-sm btn-danger"
                                onClick={() => handleStop(app.id)}
                                disabled={actionLoading === app.id}
                              >
                                Stop
                              </button>
                            </>
                          ) : null}
                          <button
                            className="btn btn-sm btn-danger"
                            onClick={() => handleDelete(app.id, app.name)}
                            disabled={actionLoading === app.id}
                          >
                            Delete
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

        {totalPages > 1 && (
          <div className="pagination">
            <button
              className="btn btn-sm"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              Previous
            </button>
            <span>
              Page {page} of {totalPages}
            </span>
            <button
              className="btn btn-sm"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
            >
              Next
            </button>
          </div>
        )}
      </div>
    </Layout>
  );
}
