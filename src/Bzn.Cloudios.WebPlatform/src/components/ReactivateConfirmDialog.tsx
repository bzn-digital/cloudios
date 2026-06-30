import { useState } from 'react';
import { apiClient } from '../lib/api';

interface ReactivateConfirmDialogProps {
  isOpen: boolean;
  realmId: string;
  realmName: string;
  onClose: () => void;
  onSuccess: () => void;
}

export function ReactivateConfirmDialog({ isOpen, realmId, realmName, onClose, onSuccess }: ReactivateConfirmDialogProps) {
  const [reactivating, setReactivating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    try {
      setReactivating(true);
      await apiClient.reactivateRealm(realmId);
      onSuccess();
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reactivate realm');
    } finally {
      setReactivating(false);
    }
  };

  const handleClose = () => {
    setError(null);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>Reactivate Realm</h2>
        <div className="info-section">
          <p>
            Are you sure you want to reactivate the realm <strong>{realmName}</strong>?
          </p>
          <p className="info-text">
            This will restore access to all resources and resume billing for the realm.
          </p>
        </div>
        <form onSubmit={handleSubmit}>
          {error && <p className="error">{error}</p>}
          <div className="modal-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleClose}
              disabled={reactivating}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-success"
              disabled={reactivating}
            >
              {reactivating ? 'Reactivating...' : 'Reactivate'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
