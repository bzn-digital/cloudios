import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { RealmDetail } from '../types/realm';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

export function RealmDetail() {
  const { id } = useParams<{ id: string }>();
  const [realm, setRealm] = useState<RealmDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'resources' | 'users' | 'billing' | 'settings'>('resources');
  const [editingQuotas, setEditingQuotas] = useState(false);
  const [quotas, setQuotas] = useState<Record<string, number>>({});
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadRealmDetail();
  }, [id]);

  const loadRealmDetail = async () => {
    if (!id) return;
    
    try {
      setLoading(true);
      setError(null);
      const data = await apiClient.getRealmDetail(id);
      setRealm(data);
      setQuotas({
        maxContainers: data.quotas.maxContainers || 0,
        maxDatabases: data.quotas.maxDatabases || 0,
        maxManagedApps: data.quotas.maxManagedApps || 0,
        maxRamBytes: data.quotas.maxRamBytes || 0,
        maxCpuCores: data.quotas.maxCpuCores || 0,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load realm details');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleStatus = async () => {
    if (!realm || !id) return;
    
    try {
      setSaving(true);
      await apiClient.updateRealm(id, { isActive: !realm.isActive });
      await loadRealmDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update realm status');
      alert(err instanceof Error ? err.message : 'Failed to update realm status');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveQuotas = async () => {
    if (!id) return;
    
    try {
      setSaving(true);
      await apiClient.updateRealm(id, { quotas });
      setEditingQuotas(false);
      await loadRealmDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update quotas');
      alert(err instanceof Error ? err.message : 'Failed to update quotas');
    } finally {
      setSaving(false);
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${Math.round((bytes / Math.pow(k, i)) * 100) / 100} ${sizes[i]}`;
  };

  if (loading) {
    return (
      <Layout>
        <div className="services">
          <h1>Realm Details</h1>
          <p>Loading...</p>
        </div>
      </Layout>
    );
  }

  if (error || !realm) {
    return (
      <Layout>
        <div className="services">
          <h1>Realm Details</h1>
          <p className="error">{error || 'Realm not found'}</p>
          <Link to="/realms" className="btn btn-secondary">
            Back to Realms
          </Link>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="services">
        <div className="realm-detail-header">
          <div>
            <h1>{realm.name}</h1>
            <div className="realm-meta">
              <code className="slug-badge">{realm.slug}</code>
              <span className={`status-badge status-${realm.isActive ? 'active' : 'suspended'}`}>
                {realm.isActive ? 'Active' : 'Suspended'}
              </span>
              <span className="created-date">
                Created: {new Date(realm.createdAt).toLocaleDateString()}
              </span>
            </div>
          </div>
          <Link to="/realms" className="btn btn-secondary">
            Back to Realms
          </Link>
        </div>

        <div className="stats-cards">
          <div className="stat-card">
            <h3>Total Users</h3>
            <p className="stat-value">{realm.stats.totalUsers}</p>
          </div>
          <div className="stat-card">
            <h3>Active Containers</h3>
            <p className="stat-value">{realm.stats.activeContainers}</p>
          </div>
          <div className="stat-card">
            <h3>Active Databases</h3>
            <p className="stat-value">{realm.stats.activeDatabases}</p>
          </div>
          <div className="stat-card">
            <h3>Month Cost</h3>
            <p className="stat-value">{formatCurrency(realm.stats.monthCost)}</p>
          </div>
        </div>

        <div className="tabs">
          <button
            className={`tab ${activeTab === 'resources' ? 'active' : ''}`}
            onClick={() => setActiveTab('resources')}
          >
            Resources
          </button>
          <button
            className={`tab ${activeTab === 'users' ? 'active' : ''}`}
            onClick={() => setActiveTab('users')}
          >
            Users
          </button>
          <button
            className={`tab ${activeTab === 'billing' ? 'active' : ''}`}
            onClick={() => setActiveTab('billing')}
          >
            Billing
          </button>
          <button
            className={`tab ${activeTab === 'settings' ? 'active' : ''}`}
            onClick={() => setActiveTab('settings')}
          >
            Settings
          </button>
        </div>

        <div className="tab-content">
          {activeTab === 'resources' && (
            <div className="tab-panel">
              <h2>Resources</h2>
              <div className="services-table">
                <table>
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Type</th>
                      <th>Status</th>
                      <th>Cost</th>
                    </tr>
                  </thead>
                  <tbody>
                    {realm.resources.length === 0 ? (
                      <tr>
                        <td colSpan={4}>
                          <p className="empty-state">No resources found.</p>
                        </td>
                      </tr>
                    ) : (
                      realm.resources.map((resource) => (
                        <tr key={resource.id}>
                          <td>{resource.name}</td>
                          <td>
                            <span className={`type-badge type-${resource.type}`}>
                              {resource.type}
                            </span>
                          </td>
                          <td>
                            <span className={`status-badge status-${resource.status.toLowerCase()}`}>
                              {resource.status}
                            </span>
                          </td>
                          <td>{formatCurrency(resource.costBRL)}</td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {activeTab === 'users' && (
            <div className="tab-panel">
              <h2>Users</h2>
              <div className="services-table">
                <table>
                  <thead>
                    <tr>
                      <th>Email</th>
                      <th>Role</th>
                      <th>Status</th>
                      <th>Created</th>
                    </tr>
                  </thead>
                  <tbody>
                    {realm.users.length === 0 ? (
                      <tr>
                        <td colSpan={4}>
                          <p className="empty-state">No users found.</p>
                        </td>
                      </tr>
                    ) : (
                      realm.users.map((user) => (
                        <tr key={user.id}>
                          <td>{user.email}</td>
                          <td>{user.role}</td>
                          <td>
                            <span className={`status-badge status-${user.status.toLowerCase()}`}>
                              {user.status}
                            </span>
                          </td>
                          <td>{new Date(user.createdAt).toLocaleDateString()}</td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {activeTab === 'billing' && (
            <div className="tab-panel">
              <h2>Billing History (Last 6 Months)</h2>
              <div className="billing-chart">
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={realm.billingHistory}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="month" />
                    <YAxis />
                    <Tooltip formatter={(value) => value !== undefined ? formatCurrency(Number(value)) : ''} />
                    <Bar dataKey="costBRL" fill="#3b82f6" />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>
          )}

          {activeTab === 'settings' && (
            <div className="tab-panel">
              <h2>Settings</h2>
              <div className="settings-section">
                <div className="settings-header">
                  <h3>Quotas</h3>
                  {!editingQuotas ? (
                    <button
                      className="btn btn-sm btn-secondary"
                      onClick={() => setEditingQuotas(true)}
                    >
                      Edit
                    </button>
                  ) : (
                    <div className="settings-actions">
                      <button
                        className="btn btn-sm btn-secondary"
                        onClick={() => {
                          setEditingQuotas(false);
                          setQuotas({
                            maxContainers: realm.quotas.maxContainers || 0,
                            maxDatabases: realm.quotas.maxDatabases || 0,
                            maxManagedApps: realm.quotas.maxManagedApps || 0,
                            maxRamBytes: realm.quotas.maxRamBytes || 0,
                            maxCpuCores: realm.quotas.maxCpuCores || 0,
                          });
                        }}
                        disabled={saving}
                      >
                        Cancel
                      </button>
                      <button
                        className="btn btn-sm btn-primary"
                        onClick={handleSaveQuotas}
                        disabled={saving}
                      >
                        {saving ? 'Saving...' : 'Save'}
                      </button>
                    </div>
                  )}
                </div>
                <div className="quota-form">
                  <div className="form-group">
                    <label>Max Containers</label>
                    <input
                      type="number"
                      value={quotas.maxContainers ?? ''}
                      onChange={(e) => setQuotas({ ...quotas, maxContainers: parseInt(e.target.value) || 0 })}
                      disabled={!editingQuotas}
                    />
                  </div>
                  <div className="form-group">
                    <label>Max Databases</label>
                    <input
                      type="number"
                      value={quotas.maxDatabases ?? ''}
                      onChange={(e) => setQuotas({ ...quotas, maxDatabases: parseInt(e.target.value) || 0 })}
                      disabled={!editingQuotas}
                    />
                  </div>
                  <div className="form-group">
                    <label>Max Managed Apps</label>
                    <input
                      type="number"
                      value={quotas.maxManagedApps ?? ''}
                      onChange={(e) => setQuotas({ ...quotas, maxManagedApps: parseInt(e.target.value) || 0 })}
                      disabled={!editingQuotas}
                    />
                  </div>
                  <div className="form-group">
                    <label>Max RAM (bytes)</label>
                    <input
                      type="number"
                      value={quotas.maxRamBytes ?? ''}
                      onChange={(e) => setQuotas({ ...quotas, maxRamBytes: parseInt(e.target.value) || 0 })}
                      disabled={!editingQuotas}
                    />
                    <span className="field-hint">{formatBytes(quotas.maxRamBytes || 0)}</span>
                  </div>
                  <div className="form-group">
                    <label>Max CPU Cores</label>
                    <input
                      type="number"
                      value={quotas.maxCpuCores ?? ''}
                      onChange={(e) => setQuotas({ ...quotas, maxCpuCores: parseInt(e.target.value) || 0 })}
                      disabled={!editingQuotas}
                    />
                  </div>
                </div>
              </div>

              <div className="settings-section">
                <h3>Realm Status</h3>
                <div className="status-actions">
                  <button
                    className={`btn ${realm.isActive ? 'btn-danger' : 'btn-success'}`}
                    onClick={handleToggleStatus}
                    disabled={saving}
                  >
                    {saving ? 'Processing...' : realm.isActive ? 'Suspend Realm' : 'Reactivate Realm'}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </Layout>
  );
}
