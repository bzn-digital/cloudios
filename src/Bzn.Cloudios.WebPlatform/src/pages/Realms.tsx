import { useState, useEffect } from 'react';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { RealmItem } from '../types/realm';

export function Realms() {
  const [realms, setRealms] = useState<RealmItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchFilter, setSearchFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [sortBy, setSortBy] = useState('name');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newRealmName, setNewRealmName] = useState('');
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    loadRealms();
  }, [searchFilter, statusFilter, sortBy, page]);

  const loadRealms = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiClient.getRealms(page, pageSize, searchFilter, statusFilter, sortBy);
      setRealms(data.items || []);
      setTotalCount(data.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load realms');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateRealm = async () => {
    if (!newRealmName.trim()) return;
    
    try {
      setCreating(true);
      await apiClient.post('/realms', { name: newRealmName });
      setNewRealmName('');
      setShowCreateModal(false);
      await loadRealms();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create realm');
      alert(err instanceof Error ? err.message : 'Failed to create realm');
    } finally {
      setCreating(false);
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  if (loading && realms.length === 0) {
    return (
      <Layout>
        <div className="services">
          <h1>Realms</h1>
          <p>Loading...</p>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Realms</h1>
          <button
            className="btn btn-primary"
            onClick={() => setShowCreateModal(true)}
          >
            New Realm
          </button>
        </div>

        <div className="services-filters">
          <input
            type="text"
            placeholder="Search by name or slug..."
            value={searchFilter}
            onChange={(e) => {
              setSearchFilter(e.target.value);
              setPage(1);
            }}
            className="search-filter"
          />
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPage(1);
            }}
            className="status-filter"
          >
            <option value="">All Status</option>
            <option value="active">Active</option>
            <option value="suspended">Suspended</option>
          </select>
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            className="sort-filter"
          >
            <option value="name">Sort by Name</option>
            <option value="createdAt">Sort by Created Date</option>
            <option value="monthlyCost">Sort by Monthly Cost</option>
          </select>
        </div>

        {error && <p className="error">{error}</p>}

        <div className="services-table">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Slug</th>
                <th>Status</th>
                <th>Users</th>
                <th>Containers</th>
                <th>Monthly Cost</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {realms.length === 0 ? (
                <tr>
                  <td colSpan={7}>
                    <p className="empty-state">No realms found.</p>
                  </td>
                </tr>
              ) : (
                realms.map((realm) => (
                  <tr key={realm.id}>
                    <td>{realm.name}</td>
                    <td>
                      <code className="slug-badge">{realm.slug}</code>
                    </td>
                    <td>
                      <span className={`status-badge status-${realm.isActive ? 'active' : 'suspended'}`}>
                        {realm.isActive ? 'Active' : 'Suspended'}
                      </span>
                    </td>
                    <td>{realm.userCount}</td>
                    <td>{realm.containerCount}</td>
                    <td>
                      {realm.monthlyCostBRL !== undefined 
                        ? formatCurrency(realm.monthlyCostBRL)
                        : 'N/A'}
                    </td>
                    <td>{new Date(realm.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))
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

      {showCreateModal && (
        <div className="modal-overlay" onClick={() => setShowCreateModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Create New Realm</h2>
            <div className="form-group">
              <label htmlFor="realmName">Realm Name</label>
              <input
                id="realmName"
                type="text"
                value={newRealmName}
                onChange={(e) => setNewRealmName(e.target.value)}
                placeholder="Enter realm name..."
                autoFocus
              />
            </div>
            <div className="modal-actions">
              <button
                className="btn btn-secondary"
                onClick={() => setShowCreateModal(false)}
                disabled={creating}
              >
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={handleCreateRealm}
                disabled={creating || !newRealmName.trim()}
              >
                {creating ? 'Creating...' : 'Create'}
              </button>
            </div>
          </div>
        </div>
      )}
    </Layout>
  );
}
