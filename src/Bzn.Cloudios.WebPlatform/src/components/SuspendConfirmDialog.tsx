import { useState } from 'react';
import { apiClient } from '../lib/api';

interface SuspendConfirmDialogProps {
  isOpen: boolean;
  realmId: string;
  realmName: string;
  onClose: () => void;
  onSuccess: () => void;
}

export function SuspendConfirmDialog({ isOpen, realmId, realmName, onClose, onSuccess }: SuspendConfirmDialogProps) {
  const [confirmation, setConfirmation] = useState('');
  const [suspending, setSuspending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (confirmation !== realmName) {
      setError('Please type the realm name exactly to confirm');
      return;
    }

    try {
      setSuspending(true);
      await apiClient.suspendRealm(realmId);
      onSuccess();
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to suspend realm');
    } finally {
      setSuspending(false);
    }
  };

  const handleClose = () => {
    setConfirmation('');
    setError(null);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>Suspend Realm</h2>
        <div className="warning-section">
          <p className="warning-text">
            <strong>Warning:</strong> Suspending this realm will have the following consequences:
          </p>
          <ul className="warning-list">
            <li>All containers will be stopped</li>
            <li>Billing will be terminated</li>
            <li>Users will lose access to their resources</li>
            <li>This action can be reversed by reactivating the realm</li>
          </ul>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="confirmation">
              Type <code>{realmName}</code> to confirm:
            </label>
            <input
              id="confirmation"
              type="text"
              value={confirmation}
              onChange={(e) => setConfirmation(e.target.value)}
              placeholder={realmName}
              autoFocus
            />
          </div>
          {error && <p className="error">{error}</p>}
          <div className="modal-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleClose}
              disabled={suspending}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-danger"
              disabled={suspending || confirmation !== realmName}
            >
              {suspending ? 'Suspending...' : 'Suspend'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
