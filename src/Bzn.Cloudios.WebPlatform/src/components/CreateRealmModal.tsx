import { useState, useEffect } from 'react';
import { apiClient } from '../lib/api';

interface CreateRealmModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export function CreateRealmModal({ isOpen, onClose, onSuccess }: CreateRealmModalProps) {
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');
  const [ownerPassword, setOwnerPassword] = useState('');
  const [isSlugUnique, setIsSlugUnique] = useState<boolean | null>(null);
  const [isEmailUnique, setIsEmailUnique] = useState<boolean | null>(null);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Auto-generate slug from name
  useEffect(() => {
    if (name) {
      const generatedSlug = name
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .trim()
        .replace(/\s+/g, '-');
      setSlug(generatedSlug);
      setIsSlugUnique(null);
    } else {
      setSlug('');
    }
  }, [name]);

  // Validate slug uniqueness
  useEffect(() => {
    if (slug && isOpen) {
      const debounceTimer = setTimeout(async () => {
        try {
          const realms = await apiClient.getRealms(1, 100);
          const exists = realms.items.some(r => r.slug === slug);
          setIsSlugUnique(!exists);
        } catch (err) {
          setIsSlugUnique(null);
        }
      }, 500);
      return () => clearTimeout(debounceTimer);
    }
  }, [slug, isOpen]);

  // Validate email uniqueness
  useEffect(() => {
    if (ownerEmail && isOpen) {
      const debounceTimer = setTimeout(async () => {
        try {
          const realms = await apiClient.getRealms(1, 100);
          // Check if email exists in any realm's users
          let emailExists = false;
          for (const realm of realms.items) {
            const detail = await apiClient.getRealmDetail(realm.id);
            if (detail.users.some(u => u.email === ownerEmail)) {
              emailExists = true;
              break;
            }
          }
          setIsEmailUnique(!emailExists);
        } catch (err) {
          setIsEmailUnique(null);
        }
      }, 500);
      return () => clearTimeout(debounceTimer);
    }
  }, [ownerEmail, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim() || !slug.trim() || !ownerEmail.trim() || !ownerPassword.trim()) {
      setError('All fields are required');
      return;
    }

    if (isSlugUnique === false) {
      setError('Slug must be unique');
      return;
    }

    if (isEmailUnique === false) {
      setError('Email already exists');
      return;
    }

    try {
      setCreating(true);
      // Create realm first
      const realm = await apiClient.createRealm({
        name: name.trim(),
        slug: slug.trim(),
        ownerEmail: ownerEmail.trim(),
        ownerPassword: ownerPassword,
      });
      // Then create owner user
      await apiClient.createUser(realm.id, {
        email: ownerEmail.trim(),
        password: ownerPassword,
        role: 'RealmOwner',
      });
      onSuccess();
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create realm');
    } finally {
      setCreating(false);
    }
  };

  const handleClose = () => {
    setName('');
    setSlug('');
    setOwnerEmail('');
    setOwnerPassword('');
    setIsSlugUnique(null);
    setIsEmailUnique(null);
    setError(null);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>Create New Realm</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="realmName">Realm Name</label>
            <input
              id="realmName"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter realm name..."
              autoFocus
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="realmSlug">Slug</label>
            <input
              id="realmSlug"
              type="text"
              value={slug}
              onChange={(e) => setSlug(e.target.value)}
              placeholder="Auto-generated from name"
              required
            />
            {isSlugUnique === false && (
              <span className="field-error">Slug already exists</span>
            )}
            {isSlugUnique === true && (
              <span className="field-success">Slug is available</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="ownerEmail">Owner Email</label>
            <input
              id="ownerEmail"
              type="email"
              value={ownerEmail}
              onChange={(e) => setOwnerEmail(e.target.value)}
              placeholder="owner@example.com"
              required
            />
            {isEmailUnique === false && (
              <span className="field-error">Email already exists</span>
            )}
            {isEmailUnique === true && (
              <span className="field-success">Email is available</span>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="ownerPassword">Owner Password</label>
            <input
              id="ownerPassword"
              type="password"
              value={ownerPassword}
              onChange={(e) => setOwnerPassword(e.target.value)}
              placeholder="Enter password..."
              required
            />
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
              disabled={creating || isSlugUnique === false || isEmailUnique === false}
            >
              {creating ? 'Creating...' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
