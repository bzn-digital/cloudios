import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ToastProvider } from './contexts/ToastContext';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import Home from './pages/Home';
import { Dashboard } from './pages/Dashboard';
import { Services } from './pages/Services';
import { NewService } from './pages/NewService';
import { ServiceDetail } from './pages/ServiceDetail';
import ManagedDatabases from './pages/ManagedDatabases';
import ManagedApps from './pages/ManagedApps';
import { ProtectedRoute } from './components/ProtectedRoute';

function App() {
  return (
    <Router>
      <ToastProvider>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <Home />
                </ProtectedRoute>
              }
            />
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
              path="/services/new"
              element={
                <ProtectedRoute>
                  <NewService />
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
              path="/managed-databases"
              element={
                <ProtectedRoute>
                  <ManagedDatabases />
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
          </Routes>
        </AuthProvider>
      </ToastProvider>
    </Router>
  );
}

export default App;
