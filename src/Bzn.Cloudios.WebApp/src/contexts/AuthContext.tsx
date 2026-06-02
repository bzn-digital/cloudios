import React, { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import type { UserInfo, LoginRequest, LoginResponse } from '../types/auth';
import { apiClient } from '../lib/api';
import { parseJwt, isTokenExpired } from '../lib/jwt';

interface AuthContextType {
  user: UserInfo | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    // Load token from localStorage on mount
    const storedToken = localStorage.getItem('token');
    if (storedToken && !isTokenExpired(storedToken)) {
      const payload = parseJwt(storedToken);
      if (payload) {
        setToken(storedToken);
        apiClient.setToken(storedToken);
        setUser({
          id: payload.sub,
          email: payload.email,
          role: payload.role,
          realmId: payload.realmId,
          realmName: payload.realmName,
        });
      }
    }
    setLoading(false);

    // Set up 401 handler
    apiClient.setUnauthorizedHandler(() => {
      logout();
      navigate('/login');
    });
  }, [navigate]);

  const login = async (credentials: LoginRequest) => {
    const response = await apiClient.post<LoginResponse>('/api/auth/login', credentials);
    setToken(response.token);
    apiClient.setToken(response.token);
    localStorage.setItem('token', response.token);
    setUser(response.user);
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    apiClient.clearToken();
    localStorage.removeItem('token');
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token,
        login,
        logout,
        loading,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
