import { useState } from 'react';
import { apiClient } from '../lib/api';

interface CreateUserModalProps {
  realmId: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export function CreateUserModal({ realmId, isOpen, onClose, onSuccess }: CreateUserModalProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState('RealmUser');
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const generatePassword = () => {
    const chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*';
    const array = new Uint32Array(16);
    crypto.getRandomValues(array);
    const password = Array.from(array, (x) => chars[x % chars.length]).join('');
    setPassword(password);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!email.trim() || !password.trim()) {
      setError('Email and password are required');
      return;
    }

    try {
      setCreating(true);
      await apiClient.createUser(realmId, {
        email: email.trim(),
        password: password,
        role,
      });
      onSuccess();
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create user');
    } finally {
      setCreating(false);
    }
  };

  const handleClose = () => {
    setEmail('');
    setPassword('');
    setRole('RealmUser');
    setError(null);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>Create New User</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="userEmail">Email</label>
            <input
              id="userEmail"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="user@example.com"
              autoFocus
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="userRole">Role</label>
            <select
              id="userRole"
              className="filter-select"
              value={role}
              onChange={(e) => setRole(e.target.value)}
              required
            >
              <option value="RealmOwner">RealmOwner</option>
              <option value="RealmAdmin">RealmAdmin</option>
              <option value="RealmUser">RealmUser</option>
              <option value="RealmSre">RealmSre</option>
            </select>
          </div>
          <div className="form-group">
            <label htmlFor="userPassword">Password</label>
            <div style={{ display: 'flex', gap: '8px' }}>
              <input
                id="userPassword"
                type="text"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter password or click Generate..."
                required
                style={{ flex: 1 }}
              />
              <button
                type="button"
                className="btn btn-secondary"
                onClick={generatePassword}
                title="Generate random password"
              >
                Generate
              </button>
            </div>
          </div>
          {error && <p className="error">{error}</p>}
          <div className="modal-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleClose}
              disabled={creating}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={creating}
            >
              {creating ? 'Creating...' : 'Create User'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
