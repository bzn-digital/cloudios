import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { CreateContainerRequest } from '../types/container';

export function NewService() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [networks, setNetworks] = useState<string[]>([]);
  const [networksLoading, setNetworksLoading] = useState(false);
  const [formData, setFormData] = useState<CreateContainerRequest>({
    name: '',
    imageName: '',
    internalPort: 80,
    hostPort: undefined,
    networkName: '',
    cpuLimitCores: 0.5,
    memoryLimitBytes: 256 * 1024 * 1024, // 256MB
    costPerHourBRL: 0.02,
    environmentVariables: {},
  });
  const [envVars, setEnvVars] = useState<{ key: string; value: string }[]>([{ key: '', value: '' }]);

  useEffect(() => {
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
    loadNetworks();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      setError(null);

      // Validate name (lowercase letters and hyphens only)
      if (!/^[a-z0-9-]+$/.test(formData.name)) {
        setError('Service name must contain only lowercase letters, numbers, and hyphens');
        return;
      }

      // Build environment variables from form
      const envVarsObj: Record<string, string> = {};
      envVars.forEach((env) => {
        if (env.key && env.value) {
          envVarsObj[env.key] = env.value;
        }
      });

      const payload = {
        ...formData,
        environmentVariables: envVarsObj,
      };

      await apiClient.createContainer(payload);
      navigate('/services');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create service');
    } finally {
      setLoading(false);
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

  return (
    <Layout>
      <div className="new-service">
        <div className="new-service-header">
          <h1>New Service</h1>
          <button className="btn btn-secondary" onClick={() => navigate('/services')}>
            Cancel
          </button>
        </div>

        {error && <p className="error">{error}</p>}

        <form onSubmit={handleSubmit} className="new-service-form">
          <div className="form-group">
            <label htmlFor="name">Service Name *</label>
            <input
              id="name"
              type="text"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="my-service"
              required
              pattern="[a-z0-9-]+"
              title="Only lowercase letters, numbers, and hyphens"
            />
            <small>Only lowercase letters, numbers, and hyphens (e.g., my-service)</small>
          </div>

          <div className="form-group">
            <label htmlFor="imageName">Docker Image *</label>
            <input
              id="imageName"
              type="text"
              value={formData.imageName}
              onChange={(e) => setFormData({ ...formData, imageName: e.target.value })}
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
              value={formData.internalPort}
              onChange={(e) => setFormData({ ...formData, internalPort: parseInt(e.target.value) || 80 })}
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
              value={formData.hostPort || ''}
              onChange={(e) => setFormData({ ...formData, hostPort: e.target.value ? parseInt(e.target.value) : undefined })}
              min={1}
              max={65535}
            />
            <small>Port to expose on host (leave empty for random port)</small>
          </div>

          <div className="form-group">
            <label htmlFor="networkName">Network Name</label>
            <select
              id="networkName"
              value={formData.networkName}
              onChange={(e) => setFormData({ ...formData, networkName: e.target.value })}
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
                value={formData.cpuLimitCores}
                onChange={(e) => setFormData({ ...formData, cpuLimitCores: parseFloat(e.target.value) || 0.5 })}
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
                value={formData.memoryLimitBytes / (1024 * 1024)}
                onChange={(e) => setFormData({ ...formData, memoryLimitBytes: (parseInt(e.target.value) || 256) * 1024 * 1024 })}
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
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Creating...' : 'Create Service'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => navigate('/services')}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    </Layout>
  );
}
