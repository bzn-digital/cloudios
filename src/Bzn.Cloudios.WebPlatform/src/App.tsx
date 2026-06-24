import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Services } from './pages/Services';
import { ServiceDetail } from './pages/ServiceDetail';
import { ManagedApps } from './pages/ManagedApps';
import { Realms } from './pages/Realms';
import { ProtectedRoute } from './components/ProtectedRoute';

function App() {
  return (
    <Router>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            }
          />
          <Route
            path="/services"
            element={
              <ProtectedRoute>
                <Services />
              </ProtectedRoute>
            }
          />
          <Route
            path="/services/:id"
            element={
              <ProtectedRoute>
                <ServiceDetail />
              </ProtectedRoute>
            }
          />
          <Route
            path="/managed-apps"
            element={
              <ProtectedRoute>
                <ManagedApps />
              </ProtectedRoute>
            }
          />
          <Route
            path="/realms"
            element={
              <ProtectedRoute>
                <Realms />
              </ProtectedRoute>
            }
          />
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </AuthProvider>
    </Router>
  );
}

export default App;
