import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { RealmQuotaEditor } from '../components/RealmQuotaEditor';
import { SuspendConfirmDialog } from '../components/SuspendConfirmDialog';
import { ReactivateConfirmDialog } from '../components/ReactivateConfirmDialog';
import { CreateUserModal } from '../components/CreateUserModal';
import { apiClient } from '../lib/api';
import type { RealmDetail, RealmStatsResponse } from '../types/realm';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

export function RealmDetail() {
  const { id } = useParams<{ id: string }>();
  const [realm, setRealm] = useState<RealmDetail | null>(null);
  const [realmStats, setRealmStats] = useState<RealmStatsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'resources' | 'users' | 'billing' | 'settings'>('resources');
  const [showQuotaEditor, setShowQuotaEditor] = useState(false);
  const [showSuspendDialog, setShowSuspendDialog] = useState(false);
  const [showReactivateDialog, setShowReactivateDialog] = useState(false);
  const [showCreateUserModal, setShowCreateUserModal] = useState(false);

  useEffect(() => {
    loadRealmDetail();
  }, [id]);

  const loadRealmDetail = async () => {
    if (!id) return;

    try {
      setLoading(true);
      setError(null);
      const [detailData, statsData] = await Promise.all([
        apiClient.getRealmDetail(id),
        apiClient.getRealmStats(id),
      ]);
      setRealm(detailData);
      setRealmStats(statsData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load realm details');
    } finally {
      setLoading(false);
    }
  };

  const loadRealmStats = async () => {
    if (!id) return;

    try {
      const statsData = await apiClient.getRealmStats(id);
      setRealmStats(statsData);
    } catch (err) {
      console.error('Failed to load realm stats:', err);
    }
  };

  const handleSuspend = () => {
    setShowSuspendDialog(true);
  };

  const handleReactivate = () => {
    setShowReactivateDialog(true);
  };

  const handleToggleUserBlock = async (userId: string, currentBlocked: boolean) => {
    if (!id) return;

    try {
      await apiClient.updateUser(id, userId, { isBlocked: !currentBlocked });
      await loadRealmDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update user status');
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

  const formatDate = (dateInput: string | Date) => {
    try {
      const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
      // Check for invalid dates or default DateTime (0001-01-01)
      if (isNaN(date.getTime()) || date.getFullYear() < 2000) return 'N/A';
      return date.toLocaleDateString('pt-BR');
    } catch {
      return 'N/A';
    }
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
              {realm.ownerEmail && (
                <span className="owner-email">
                  Owner: {realm.ownerEmail}
                </span>
              )}
            </div>
          </div>
          <Link to="/realms" className="btn btn-secondary">
            Back to Realms
          </Link>
        </div>

        <div className="stats-cards">
          <div className="stat-card">
            <h3>Total Users</h3>
            <p className="stat-value">{realmStats?.usersCount || 0}</p>
          </div>
          <div className="stat-card">
            <h3>Active Containers</h3>
            <p className="stat-value">{realmStats?.containersCount || 0}</p>
          </div>
          <div className="stat-card">
            <h3>Active Databases</h3>
            <p className="stat-value">{realmStats?.databasesCount || 0}</p>
          </div>
          <div className="stat-card">
            <h3>Month Cost</h3>
            <p className="stat-value">{formatCurrency(realmStats?.monthlyCostBRL || 0)}</p>
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
              {realm.resources && realm.resources.length > 0 && (
                <div className="resource-stats">
                  <div className="resource-stat-item">
                    <div className="resource-stat-label">Total Resources</div>
                    <div className="resource-stat-value">{realm.resources.length}</div>
                  </div>
                  <div className="resource-stat-item">
                    <div className="resource-stat-label">Containers</div>
                    <div className="resource-stat-value">{realm.resources.filter(r => r.type === 'container').length}</div>
                  </div>
                  <div className="resource-stat-item">
                    <div className="resource-stat-label">Databases</div>
                    <div className="resource-stat-value">{realm.resources.filter(r => r.type === 'database').length}</div>
                  </div>
                  <div className="resource-stat-item">
                    <div className="resource-stat-label">Managed Apps</div>
                    <div className="resource-stat-value">{realm.resources.filter(r => r.type === 'managedapp').length}</div>
                  </div>
                </div>
              )}
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
                    {(!realm.resources || realm.resources.length === 0) ? (
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
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                <h2>Users</h2>
                <button
                  className="btn btn-primary"
                  onClick={() => setShowCreateUserModal(true)}
                >
                  Add User
                </button>
              </div>
              <div className="services-table">
                <table>
                  <thead>
                    <tr>
                      <th>Email</th>
                      <th>Role</th>
                      <th>Status</th>
                      <th>Created</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(!realm.users || realm.users.length === 0) ? (
                      <tr>
                        <td colSpan={5}>
                          <p className="empty-state">No users found.</p>
                        </td>
                      </tr>
                    ) : (
                      realm.users.map((user) => (
                        <tr key={user.id}>
                          <td>{user.email}</td>
                          <td>{user.role}</td>
                          <td>
                            <span className={`status-badge status-${user.isBlocked ? 'suspended' : 'active'}`}>
                              {user.isBlocked ? 'Blocked' : 'Active'}
                            </span>
                          </td>
                          <td>{formatDate(user.createdAt)}</td>
                          <td>
                            <button
                              className={`btn btn-sm ${user.isBlocked ? 'btn-success' : 'btn-danger'}`}
                              onClick={() => handleToggleUserBlock(user.id, user.isBlocked)}
                            >
                              {user.isBlocked ? 'Unblock' : 'Block'}
                            </button>
                          </td>
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
                  <BarChart data={realm.billingHistory || []}>
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
                  <button
                    className="btn btn-sm btn-secondary"
                    onClick={() => setShowQuotaEditor(true)}
                  >
                    Edit
                  </button>
                </div>
                <div className="quota-display">
                  <div className="quota-item">
                    <span>Max Containers:</span>
                    <strong>{realmStats?.quotas?.maxContainers ?? 'Unlimited'}</strong>
                  </div>
                  <div className="quota-item">
                    <span>Max Databases:</span>
                    <strong>{realmStats?.quotas?.maxDatabases ?? 'Unlimited'}</strong>
                  </div>
                  <div className="quota-item">
                    <span>Max Managed Apps:</span>
                    <strong>{realmStats?.quotas?.maxManagedApps ?? 'Unlimited'}</strong>
                  </div>
                  <div className="quota-item">
                    <span>Max RAM:</span>
                    <strong>{realmStats?.quotas?.maxRamBytes ? formatBytes(realmStats.quotas.maxRamBytes) : 'Unlimited'}</strong>
                  </div>
                  <div className="quota-item">
                    <span>Max CPU Cores:</span>
                    <strong>{realmStats?.quotas?.maxCpuCores ?? 'Unlimited'}</strong>
                  </div>
                </div>
              </div>

              <div className="settings-section">
                <h3>Realm Status</h3>
                <div className="status-actions">
                  {realm.isActive ? (
                    <button
                      className="btn btn-danger"
                      onClick={handleSuspend}
                    >
                      Suspend Realm
                    </button>
                  ) : (
                    <button
                      className="btn btn-success"
                      onClick={handleReactivate}
                    >
                      Reactivate Realm
                    </button>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {showQuotaEditor && realmStats && (
        <RealmQuotaEditor
          realmId={id!}
          quotas={realmStats.quotas}
          usage={realmStats.usage}
          isOpen={showQuotaEditor}
          onClose={() => setShowQuotaEditor(false)}
          onSuccess={loadRealmStats}
        />
      )}

      {showSuspendDialog && realm && (
        <SuspendConfirmDialog
          isOpen={showSuspendDialog}
          realmId={id!}
          realmName={realm.name}
          onClose={() => setShowSuspendDialog(false)}
          onSuccess={loadRealmDetail}
        />
      )}

      {showReactivateDialog && realm && (
        <ReactivateConfirmDialog
          isOpen={showReactivateDialog}
          realmId={id!}
          realmName={realm.name}
          onClose={() => setShowReactivateDialog(false)}
          onSuccess={loadRealmDetail}
        />
      )}

      {showCreateUserModal && (
        <CreateUserModal
          realmId={id!}
          isOpen={showCreateUserModal}
          onClose={() => setShowCreateUserModal(false)}
          onSuccess={loadRealmDetail}
        />
      )}
    </Layout>
  );
}
