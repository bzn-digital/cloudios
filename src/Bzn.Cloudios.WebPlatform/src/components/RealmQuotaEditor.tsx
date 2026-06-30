import { useState } from 'react';
import { apiClient } from '../lib/api';
import type { RealmQuotas, RealmUsage } from '../types/realm';

interface RealmQuotaEditorProps {
  realmId: string;
  quotas?: RealmQuotas;
  usage?: RealmUsage;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export function RealmQuotaEditor({ realmId, quotas, usage, isOpen, onClose, onSuccess }: RealmQuotaEditorProps) {
  const [maxContainers, setMaxContainers] = useState(quotas?.maxContainers || 0);
  const [maxDatabases, setMaxDatabases] = useState(quotas?.maxDatabases || 0);
  const [maxManagedApps, setMaxManagedApps] = useState(quotas?.maxManagedApps || 0);
  const [maxRamGB, setMaxRamGB] = useState(((quotas?.maxRamBytes || 0) / (1024 * 1024 * 1024)).toFixed(2));
  const [maxCpuCores, setMaxCpuCores] = useState(quotas?.maxCpuCores || 0);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${Math.round((bytes / Math.pow(k, i)) * 100) / 100} ${sizes[i]}`;
  };

  const getProgressPercent = (used: number, max: number) => {
    if (max === 0) return 0;
    return Math.min((used / max) * 100, 100);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    try {
      setSaving(true);
      const ramBytes = Math.round((parseFloat(maxRamGB) || 0) * 1024 * 1024 * 1024);
      await apiClient.updateQuotas(realmId, {
        maxContainers: maxContainers,
        maxDatabases: maxDatabases,
        maxManagedApps: maxManagedApps,
        maxRamBytes: ramBytes,
        maxCpuCores: maxCpuCores,
      });
      onSuccess();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update quotas');
    } finally {
      setSaving(false);
    }
  };

  const handleClose = () => {
    setError(null);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal modal-lg" onClick={(e) => e.stopPropagation()}>
        <h2>Edit Realm Quotas</h2>
        <form onSubmit={handleSubmit}>
          <div className="quota-section">
            <div className="form-group">
              <label htmlFor="maxContainers">Max Containers</label>
              <input
                id="maxContainers"
                type="number"
                min="0"
                value={maxContainers}
                onChange={(e) => setMaxContainers(parseInt(e.target.value) || 0)}
              />
              <div className="progress-bar-container">
                <div className="progress-bar">
                  <div
                    className="progress-fill"
                    style={{ width: `${getProgressPercent(usage?.containersCount || 0, maxContainers)}%` }}
                  />
                </div>
                <span className="progress-text">
                  {usage?.containersCount || 0} / {maxContainers}
                </span>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="maxDatabases">Max Databases</label>
              <input
                id="maxDatabases"
                type="number"
                min="0"
                value={maxDatabases}
                onChange={(e) => setMaxDatabases(parseInt(e.target.value) || 0)}
              />
              <div className="progress-bar-container">
                <div className="progress-bar">
                  <div
                    className="progress-fill"
                    style={{ width: `${getProgressPercent(usage?.databasesCount || 0, maxDatabases)}%` }}
                  />
                </div>
                <span className="progress-text">
                  {usage?.databasesCount || 0} / {maxDatabases}
                </span>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="maxManagedApps">Max Managed Apps</label>
              <input
                id="maxManagedApps"
                type="number"
                min="0"
                value={maxManagedApps}
                onChange={(e) => setMaxManagedApps(parseInt(e.target.value) || 0)}
              />
              <div className="progress-bar-container">
                <div className="progress-bar">
                  <div
                    className="progress-fill"
                    style={{ width: `${getProgressPercent(usage?.managedAppsCount || 0, maxManagedApps)}%` }}
                  />
                </div>
                <span className="progress-text">
                  {usage?.managedAppsCount || 0} / {maxManagedApps}
                </span>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="maxRamGB">Max RAM (GB)</label>
              <input
                id="maxRamGB"
                type="number"
                min="0"
                step="0.1"
                value={maxRamGB}
                onChange={(e) => setMaxRamGB(e.target.value)}
              />
              <div className="progress-bar-container">
                <div className="progress-bar">
                  <div
                    className="progress-fill"
                    style={{ width: `${getProgressPercent(usage?.ramBytesUsed || 0, Math.round(parseFloat(maxRamGB) * 1024 * 1024 * 1024))}%` }}
                  />
                </div>
                <span className="progress-text">
                  {formatBytes(usage?.ramBytesUsed || 0)} / {maxRamGB} GB
                </span>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="maxCpuCores">Max CPU Cores</label>
              <input
                id="maxCpuCores"
                type="number"
                min="0"
                step="0.5"
                value={maxCpuCores}
                onChange={(e) => setMaxCpuCores(parseFloat(e.target.value) || 0)}
              />
              <div className="progress-bar-container">
                <div className="progress-bar">
                  <div
                    className="progress-fill"
                    style={{ width: `${getProgressPercent(usage?.cpuCoresUsed || 0, maxCpuCores)}%` }}
                  />
                </div>
                <span className="progress-text">
                  {usage?.cpuCoresUsed || 0} / {maxCpuCores}
                </span>
              </div>
            </div>
          </div>

          {error && <p className="error">{error}</p>}

          <div className="modal-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleClose}
              disabled={saving}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={saving}
            >
              {saving ? 'Saving...' : 'Save Quotas'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
