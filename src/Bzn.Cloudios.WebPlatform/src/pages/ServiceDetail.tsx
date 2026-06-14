import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { ContainerDetailResponse, ContainerLogEntry } from '../types/container';

export function ServiceDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [container, setContainer] = useState<ContainerDetailResponse | null>(null);
  const [logs, setLogs] = useState<ContainerLogEntry[]>([]);
  const [metrics, setMetrics] = useState<{ cpu: number; memory: number } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'details' | 'logs' | 'metrics'>('details');
  const [autoRefresh, setAutoRefresh] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    if (id) {
      loadContainer();
    }
  }, [id]);

  useEffect(() => {
    let interval: number;
    if (autoRefresh && activeTab === 'logs' && id) {
      loadLogs();
      interval = window.setInterval(() => loadLogs(), 5000);
    }
    return () => {
      if (interval) window.clearInterval(interval);
    };
  }, [autoRefresh, activeTab, id]);

  useEffect(() => {
    let interval: number;
    if (activeTab === 'metrics' && id) {
      loadMetrics();
      interval = window.setInterval(() => loadMetrics(), 2000);
    }
    return () => {
      if (interval) window.clearInterval(interval);
    };
  }, [activeTab, id]);

  const loadContainer = async () => {
    if (!id) return;
    try {
      setLoading(true);
      const data = await apiClient.getContainer(id) as ContainerDetailResponse;
      setContainer(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load container');
    } finally {
      setLoading(false);
    }
  };

  const loadLogs = async () => {
    if (!id) return;
    try {
      const data = await apiClient.getContainerLogs(id, 100) as { logs: ContainerLogEntry[] };
      setLogs(data.logs || []);
    } catch (err) {
      console.error('Failed to load logs:', err);
    }
  };

  const loadMetrics = async () => {
    if (!id) return;
    try {
      const data = await apiClient.getContainerMetrics(id) as { cpuPercent: number; memoryUsedBytes: number };
      setMetrics({
        cpu: data.cpuPercent,
        memory: data.memoryUsedBytes
      });
    } catch (err) {
      console.error('Failed to load metrics:', err);
    }
  };

  const handleAction = async (action: () => Promise<unknown>) => {
    try {
      setActionLoading(true);
      await action();
      await loadContainer();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed');
    } finally {
      setActionLoading(false);
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

  if (loading) {
    return (
      <Layout>
        <div className="service-detail">
          <h1>Service Details</h1>
          <p>Loading...</p>
        </div>
      </Layout>
    );
  }

  if (error || !container) {
    return (
      <Layout>
        <div className="service-detail">
          <h1>Service Details</h1>
          <p className="error">{error || 'Container not found'}</p>
          <button className="btn btn-secondary" onClick={() => navigate('/services')}>
            Back to Services
          </button>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="service-detail">
        <div className="service-detail-header">
          <div>
            <h1>{container.name}</h1>
            <p className="service-detail-image">{container.imageName}</p>
          </div>
          <div className="service-detail-actions">
            <span className={`status-badge status-${container.status.toLowerCase()}`}>
              {container.status}
            </span>
            {container.status === 'Running' ? (
              <>
                <button
                  className="btn btn-sm btn-warning"
                  onClick={() => handleAction(() => apiClient.restartContainer(id!))}
                  disabled={actionLoading}
                >
                  Restart
                </button>
                <button
                  className="btn btn-sm btn-danger"
                  onClick={() => handleAction(() => apiClient.stopContainer(id!))}
                  disabled={actionLoading}
                >
                  Stop
                </button>
              </>
            ) : (
              <button
                className="btn btn-sm btn-success"
                onClick={() => handleAction(() => apiClient.startContainer(id!))}
                disabled={actionLoading}
              >
                Start
              </button>
            )}
          </div>
        </div>

        <div className="service-detail-tabs">
          <button
            className={activeTab === 'details' ? 'active' : ''}
            onClick={() => setActiveTab('details')}
          >
            Details
          </button>
          <button
            className={activeTab === 'logs' ? 'active' : ''}
            onClick={() => {
              setActiveTab('logs');
              loadLogs();
            }}
          >
            Logs
          </button>
          <button
            className={activeTab === 'metrics' ? 'active' : ''}
            onClick={() => {
              setActiveTab('metrics');
              loadMetrics();
            }}
          >
            Metrics
          </button>
        </div>

        {activeTab === 'details' && (
          <div className="service-detail-content">
            <div className="service-detail-grid">
              <div className="service-detail-card">
                <h3>Status</h3>
                <p>{container.status}</p>
              </div>
              <div className="service-detail-card">
                <h3>CPU Limit</h3>
                <p>{formatCpu(container.cpuLimitCores)}</p>
              </div>
              <div className="service-detail-card">
                <h3>RAM Limit</h3>
                <p>{formatBytes(container.memoryLimitBytes)}</p>
              </div>
              <div className="service-detail-card">
                <h3>Internal Port</h3>
                <p>{container.internalPort}</p>
              </div>
              <div className="service-detail-card">
                <h3>Cost per Hour</h3>
                <p>R$ {container.costPerHourBRL.toFixed(2)}</p>
              </div>
              <div className="service-detail-card">
                <h3>Cost this Month</h3>
                <p>R$ {container.currentMonthCostBRL.toFixed(2)}</p>
              </div>
            </div>

            {container.dockerContainerId && (
              <div className="service-detail-section">
                <h3>Container ID</h3>
                <code>{container.dockerContainerId}</code>
              </div>
            )}

            <div className="service-detail-section">
              <h3>Created At</h3>
              <p>{new Date(container.createdAt).toLocaleString()}</p>
            </div>

            {container.startedAtUtc && (
              <div className="service-detail-section">
                <h3>Started At</h3>
                <p>{new Date(container.startedAtUtc).toLocaleString()}</p>
              </div>
            )}

            <h3>Environment Variables</h3>
            {container.environmentVariables.length === 0 ? (
              <p>No environment variables configured.</p>
            ) : (
              <table className="env-vars-table">
                <thead>
                  <tr>
                    <th>Key</th>
                    <th>Value</th>
                  </tr>
                </thead>
                <tbody>
                  {container.environmentVariables.map((env, index) => (
                    <tr key={index}>
                      <td>{(env as any).key}</td>
                      <td>
                        <code>{(env as any).value}</code>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            <h3>Volumes</h3>
            {container.volumes.length === 0 ? (
              <p>No volumes configured.</p>
            ) : (
              <table className="volumes-table">
                <thead>
                  <tr>
                    <th>Host Path</th>
                    <th>Container Path</th>
                    <th>Read Only</th>
                  </tr>
                </thead>
                <tbody>
                  {container.volumes.map((volume) => (
                    <tr key={volume.id}>
                      <td><code>{volume.hostPath}</code></td>
                      <td><code>{volume.containerPath}</code></td>
                      <td>{volume.isReadOnly ? 'Yes' : 'No'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}

        {activeTab === 'logs' && (
          <div className="service-detail-content">
            <div className="logs-header">
              <h3>Container Logs</h3>
              <label>
                <input
                  type="checkbox"
                  checked={autoRefresh}
                  onChange={(e) => setAutoRefresh(e.target.checked)}
                />
                Auto-refresh (5s)
              </label>
            </div>
            <div className="logs-container">
              {logs.length === 0 ? (
                <p>No logs available.</p>
              ) : (
                logs.map((log, index) => (
                  <div key={index} className="log-entry">
                    <span className="log-timestamp">
                      {new Date(log.timestamp).toLocaleTimeString()}
                    </span>
                    <span className={`log-stream log-stream-${log.stream.toLowerCase()}`}>
                      {log.stream}
                    </span>
                    <span className="log-line">{log.line}</span>
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        {activeTab === 'metrics' && (
          <div className="service-detail-content">
            <h3>Real-time Metrics</h3>
            {container.status !== 'Running' ? (
              <p>Metrics are only available when the container is running.</p>
            ) : !metrics ? (
              <p>Loading metrics...</p>
            ) : (
              <div className="metrics-dashboard">
                <div className="metric-card">
                  <h4>CPU Usage</h4>
                  <div className="metric-value">
                    {metrics.cpu.toFixed(2)}%
                  </div>
                  <div className="metric-bar">
                    <div 
                      className="metric-bar-fill metric-bar-cpu"
                      style={{ width: `${Math.min(metrics.cpu, 100)}%` }}
                    />
                  </div>
                </div>
                <div className="metric-card">
                  <h4>Memory Usage</h4>
                  <div className="metric-value">
                    {formatBytes(metrics.memory)}
                  </div>
                  <div className="metric-bar">
                    <div 
                      className="metric-bar-fill metric-bar-memory"
                      style={{ width: `${Math.min((metrics.memory / container.memoryLimitBytes) * 100, 100)}%` }}
                    />
                  </div>
                  <p className="metric-limit">
                    of {formatBytes(container.memoryLimitBytes)}
                  </p>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </Layout>
  );
}
