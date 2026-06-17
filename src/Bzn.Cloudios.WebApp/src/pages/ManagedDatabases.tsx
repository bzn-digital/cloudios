import { useState, useEffect } from 'react';
import { Layout } from '../components/Layout';
import { Modal } from '../components/Modal';
import { apiClient } from '../lib/api';

interface DatabaseTier {
  id: string;
  name: string;
  cpuLimitCores: number;
  memoryLimitBytes: number;
  pricing: {
    engine: string;
    hourlyRateBRL: number;
    monthlyForecastBRL: number;
  }[];
}

interface ManagedDatabase {
  id: string;
  name: string;
  type: string;
  status: string;
  tierName: string;
  cpuLimitCores: number;
  memoryLimitBytes: number;
  hourlyRateBRL: number;
  monthlyForecastBRL: number;
  createdAt: string;
}

const ManagedDatabases = () => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [tiers, setTiers] = useState<DatabaseTier[]>([]);
  const [databases, setDatabases] = useState<ManagedDatabase[]>([]);
  const [databasesLoading, setDatabasesLoading] = useState(false);
  const [networks, setNetworks] = useState<string[]>([]);
  const [networksLoading, setNetworksLoading] = useState(false);
  const [formData, setFormData] = useState({
    instanceName: '',
    databaseType: 'mysql',
    tierId: '',
    diskSizeGB: 10,
    networkName: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    const loadTiers = async () => {
      try {
        const response = await apiClient.get('/managed-databases/tiers') as { tiers: DatabaseTier[] };
        setTiers(response.tiers || []);
      } catch (err) {
        console.error('Failed to load tiers:', err);
      }
    };
    loadTiers();
  }, []);

  useEffect(() => {
    const loadDatabases = async () => {
      try {
        setDatabasesLoading(true);
        const response = await apiClient.get('/managed-databases') as ManagedDatabase[];
        setDatabases(response || []);
      } catch (err) {
        console.error('Failed to load databases:', err);
      } finally {
        setDatabasesLoading(false);
      }
    };
    loadDatabases();
  }, []);

  useEffect(() => {
    const loadNetworks = async () => {
      try {
        setNetworksLoading(true);
        const data = await apiClient.getNetworks() as { networks: string[] };
        setNetworks(data.networks || ['default']);
      } catch (err) {
        console.error('Failed to load networks:', err);
        setNetworks(['default']);
      } finally {
        setNetworksLoading(false);
      }
    };
    loadNetworks();
  }, []);

  const selectedTier = tiers.find(t => t.id === formData.tierId);
  const tierCostPerHour = selectedTier?.pricing.find(p => p.engine.toLowerCase() === formData.databaseType.toLowerCase())?.hourlyRateBRL || 0;
  const diskCostPerGBPerHour = 0.0005; // R$ 0.0005 por GB por hora (~R$ 0.36 por GB por mês)
  const diskCostPerHour = formData.diskSizeGB * diskCostPerGBPerHour;
  const costPerHour = tierCostPerHour + diskCostPerHour;
  const costPerMonth = costPerHour * 24 * 30;

  const handleOpenModal = () => {
    setIsModalOpen(true);
    setErrors({});
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setFormData({
      instanceName: '',
      databaseType: 'mysql',
      tierId: '',
      diskSizeGB: 10,
      networkName: '',
    });
    setErrors({});
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const newErrors: Record<string, string> = {};

    if (!formData.instanceName.trim()) {
      newErrors.instanceName = 'Instance name is required';
    } else if (!/^[a-z0-9-]+$/.test(formData.instanceName)) {
      newErrors.instanceName = 'Only lowercase letters, numbers, and hyphens allowed';
    }

    if (!formData.tierId) {
      newErrors.tierId = 'Tier is required';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      const requestData = {
        name: formData.instanceName,
        tierId: formData.tierId,
        type: formData.databaseType,
        diskSizeGB: formData.diskSizeGB
      };
      await apiClient.post('/managed-databases', requestData);
      handleCloseModal();
      // Reload databases list
      const response = await apiClient.get('/managed-databases') as ManagedDatabase[];
      setDatabases(response || []);
    } catch (err: any) {
      console.error('Failed to create database:', err);
      const errorMessage = err.message || 'Failed to create database. Please try again.';
      alert(errorMessage);
    }
  };

  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Managed Databases</h1>
          <button className="btn btn-primary" onClick={handleOpenModal}>
            + Create Database
          </button>
        </div>

        <div className="services-filters">
          <input
            type="text"
            placeholder="Search databases..."
            className="search-input"
          />
          <select className="status-filter">
            <option value="All">All Status</option>
            <option value="Running">Running</option>
            <option value="Stopped">Stopped</option>
            <option value="Failed">Failed</option>
          </select>
        </div>

        <div className="services-table">
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Name</th>
                <th>Type</th>
                <th>Tier</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {databasesLoading ? (
                <tr>
                  <td colSpan={6}>
                    <p className="empty-state">Loading...</p>
                  </td>
                </tr>
              ) : databases.length === 0 ? (
                <tr>
                  <td colSpan={6}>
                    <p className="empty-state">No managed databases deployed yet.</p>
                  </td>
                </tr>
              ) : (
                databases.map((db) => (
                  <tr key={db.id}>
                    <td>
                      <span className={`status-badge status-${db.status.toLowerCase()}`}>
                        {db.status}
                      </span>
                    </td>
                    <td>{db.name}</td>
                    <td>{db.type}</td>
                    <td>{db.tierName}</td>
                    <td>{new Date(db.createdAt).toLocaleDateString()}</td>
                    <td>
                      <button className="btn btn-sm btn-secondary">View</button>
                      <button className="btn btn-sm btn-danger">Delete</button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        title="Create Managed Database"
      >
        <form onSubmit={handleSubmit} className="create-database-form">
          <div className="form-group">
            <label htmlFor="instanceName">Instance Name *</label>
            <input
              id="instanceName"
              type="text"
              value={formData.instanceName}
              onChange={(e) => setFormData({ ...formData, instanceName: e.target.value })}
              placeholder="my-database"
              className={errors.instanceName ? 'modal-input error' : 'modal-input'}
            />
            {errors.instanceName && <small className="error-text">{errors.instanceName}</small>}
            <small>Only lowercase letters, numbers, and hyphens (e.g., my-database)</small>
          </div>

          <div className="form-group">
            <label htmlFor="databaseType">Database Type *</label>
            <select
              id="databaseType"
              value={formData.databaseType}
              onChange={(e) => setFormData({ ...formData, databaseType: e.target.value })}
              className="modal-input"
            >
              <option value="mysql">MySQL</option>
              <option value="mongodb">MongoDB</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="tierId">Instance Tier *</label>
            <select
              id="tierId"
              value={formData.tierId}
              onChange={(e) => setFormData({ ...formData, tierId: e.target.value })}
              className={errors.tierId ? 'modal-input error' : 'modal-input'}
              required
            >
              <option value="">Select a tier</option>
              {tiers.map((tier) => (
                <option key={tier.id} value={tier.id}>
                  {tier.name} - {tier.cpuLimitCores} CPU, {(tier.memoryLimitBytes / (1024 * 1024 * 1024)).toFixed(1)} GB RAM
                </option>
              ))}
            </select>
            {errors.tierId && <small className="error-text">{errors.tierId}</small>}
          </div>

          <div className="form-group">
            <label htmlFor="diskSizeGB">Disk Size (SSD) *</label>
            <div className="disk-slider-container">
              <input
                id="diskSizeGB"
                type="range"
                min="10"
                max="500"
                step="10"
                value={formData.diskSizeGB}
                onChange={(e) => setFormData({ ...formData, diskSizeGB: parseInt(e.target.value) })}
                className="disk-slider"
              />
              <div className="disk-size-display">
                <span className="disk-size-value">{formData.diskSizeGB} GB</span>
              </div>
            </div>
            <small>SSD storage for your database (10GB - 500GB)</small>
          </div>

          <div className="form-group">
            <label htmlFor="networkName">Network</label>
            <select
              id="networkName"
              value={formData.networkName}
              onChange={(e) => setFormData({ ...formData, networkName: e.target.value })}
              disabled={networksLoading}
              className="modal-input"
            >
              <option value="">Default realm network</option>
              {networks.map((network) => (
                <option key={network} value={network}>
                  {network}
                </option>
              ))}
            </select>
          </div>

          <div className="billing-preview">
            <h3>Billing Preview</h3>
            {selectedTier ? (
              <div className="billing-costs">
                <div className="cost-item">
                  <span className="cost-label">Instance tier (per hour)</span>
                  <span className="cost-value">R$ {tierCostPerHour.toFixed(3)}</span>
                </div>
                <div className="cost-item">
                  <span className="cost-label">Disk {formData.diskSizeGB}GB (per hour)</span>
                  <span className="cost-value">R$ {diskCostPerHour.toFixed(3)}</span>
                </div>
                <div className="cost-item total">
                  <span className="cost-label">Total per hour</span>
                  <span className="cost-value">R$ {costPerHour.toFixed(3)}</span>
                </div>
                <div className="cost-item total">
                  <span className="cost-label">Total per month (720h)</span>
                  <span className="cost-value">R$ {costPerMonth.toFixed(2)}</span>
                </div>
              </div>
            ) : (
              <p className="billing-placeholder">Select an instance tier to see pricing</p>
            )}
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={handleCloseModal}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              Create Database
            </button>
          </div>
        </form>
      </Modal>
    </Layout>
  );
};

export default ManagedDatabases;
