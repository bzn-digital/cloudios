import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { LoginRequest } from '../types/auth';

export function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [realmName, setRealmName] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const credentials: LoginRequest = { email, password, realmName };
      await login(credentials);
      navigate('/dashboard');
    } catch (err) {
      setError('Login failed. Please check your credentials.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <div className="login-card">
        <div className="login-header">
          <h1>Cloudios</h1>
          <p>Sign in to your realm</p>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          {error && (
            <div className="login-error">
              {error}
            </div>
          )}

          <div className="form-group">
            <label htmlFor="realmName">Realm</label>
            <input
              id="realmName"
              type="text"
              value={realmName}
              onChange={(e) => setRealmName(e.target.value)}
              placeholder="Enter your realm name"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="login-button"
          >
            {loading ? 'Signing in...' : 'Sign in'}
          </button>
        </form>

        <p style={{ textAlign: 'center', marginTop: '20px', fontSize: '14px', color: 'var(--bzn-gray-500)' }}>
          Don't have an account?{' '}
          <button
            type="button"
            onClick={() => navigate('/register')}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--bzn-primary-hover)',
              cursor: 'pointer',
              textDecoration: 'none',
              padding: 0,
              fontSize: '14px',
              fontWeight: 600,
            }}
          >
            Sign up
          </button>
        </p>
      </div>
    </div>
  );
}
