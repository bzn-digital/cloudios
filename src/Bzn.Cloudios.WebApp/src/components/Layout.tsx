import type { ReactNode } from 'react';
import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Breadcrumb } from './Breadcrumb';
import { Header } from './Header';

interface LayoutProps {
  children: ReactNode;
}

interface MenuItem {
  id: string;
  label: string;
  icon: React.ReactNode;
  path?: string;
  children?: MenuItem[];
}

const icons = {
  home: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
      <polyline points="9 22 9 12 15 12 15 22" />
    </svg>
  ),
  dashboard: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" />
    </svg>
  ),
  computing: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
      <line x1="8" y1="21" x2="16" y2="21" />
      <line x1="12" y1="17" x2="12" y2="21" />
    </svg>
  ),
  services: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M22 12h-4l-3 9L9 3l-3 9H2" />
    </svg>
  ),
  databases: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <ellipse cx="12" cy="5" rx="9" ry="3" />
      <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3" />
      <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5" />
    </svg>
  ),
  apps: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 2L2 7l10 5 10-5-10-5z" />
      <path d="M2 17l10 5 10-5" />
      <path d="M2 12l10 5 10-5" />
    </svg>
  ),
  billing: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="12" y1="1" x2="12" y2="23" /><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
    </svg>
  ),
  networking: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="2" width="20" height="8" rx="2" ry="2" />
      <rect x="2" y="14" width="20" height="8" rx="2" ry="2" />
      <line x1="6" y1="6" x2="6.01" y2="6" />
      <line x1="6" y1="18" x2="6.01" y2="18" />
    </svg>
  ),
  virtualNetworks: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M20 17.58A5 5 0 0 0 18 8h-1.26A8 8 0 1 0 4 16.25" />
      <line x1="8" y1="16" x2="8.01" y2="16" />
      <line x1="8" y1="20" x2="8.01" y2="20" />
      <line x1="12" y1="18" x2="12.01" y2="18" />
      <line x1="12" y1="22" x2="12.01" y2="22" />
      <line x1="16" y1="16" x2="16.01" y2="16" />
      <line x1="16" y1="20" x2="16.01" y2="20" />
    </svg>
  ),
  domains: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10" />
      <line x1="2" y1="12" x2="22" y2="12" />
      <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
    </svg>
  ),
  security: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
      <path d="M7 11V7a5 5 0 0 1 10 0v4" />
    </svg>
  ),
  accounts: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
      <circle cx="12" cy="7" r="4" />
    </svg>
  ),
  groups: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
      <path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  ),
  roleRules: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
    </svg>
  ),
  analytics: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="18" y1="20" x2="18" y2="10" />
      <line x1="12" y1="20" x2="12" y2="4" />
      <line x1="6" y1="20" x2="6" y2="14" />
    </svg>
  ),
  invoices: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <polyline points="14 2 14 8 20 8" />
      <line x1="16" y1="13" x2="8" y2="13" />
      <line x1="16" y1="17" x2="8" y2="17" />
      <polyline points="10 9 9 9 8 9" />
    </svg>
  ),
  billingManagement: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="1" y="4" width="22" height="16" rx="2" ry="2" />
      <line x1="1" y1="10" x2="23" y2="10" />
    </svg>
  ),
  team: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  ),
  chevronDown: (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="6 9 12 15 18 9" />
    </svg>
  ),
  chevronRight: (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="9 18 15 12 9 6" />
    </svg>
  ),
  pin: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="12" y1="17" x2="12" y2="22" />
      <path d="M5 17h14v-1.76a2 2 0 0 0-1.11-1.79l-1.78-.9A2 2 0 0 1 15 10.76V6h1a2 2 0 0 0 0-4H8a2 2 0 0 0 0 4h1v4.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24Z" />
    </svg>
  ),
  pinFilled: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="12" y1="17" x2="12" y2="22" />
      <path d="M5 17h14v-1.76a2 2 0 0 0-1.11-1.79l-1.78-.9A2 2 0 0 1 15 10.76V6h1a2 2 0 0 0 0-4H8a2 2 0 0 0 0 4h1v4.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24Z" />
    </svg>
  ),
};

export function Layout({ children }: LayoutProps) {
  const { user, logout } = useAuth();
  const location = useLocation();
  const savedPin = localStorage.getItem('sidebar-pinned') === 'true';
  const [isCollapsed, setIsCollapsed] = useState(!savedPin);
  const [isPinned, setIsPinned] = useState(savedPin);
  const [expandedMenus, setExpandedMenus] = useState<Set<string>>(new Set(['computing']));

  const togglePin = () => {
    const newPinned = !isPinned;
    setIsPinned(newPinned);
    localStorage.setItem('sidebar-pinned', String(newPinned));
    if (newPinned) {
      setIsCollapsed(false);
    }
  };

  const toggleMenu = (menuId: string) => {
    setExpandedMenus((prev) => {
      const next = new Set(prev);
      if (next.has(menuId)) {
        next.delete(menuId);
      } else {
        next.add(menuId);
      }
      return next;
    });
  };

  const menuItems: MenuItem[] = [
    { id: 'home', label: 'Home', icon: icons.home, path: '/' },
    { id: 'dashboard', label: 'Dashboard', icon: icons.dashboard, path: '/dashboard' },
    {
      id: 'computing',
      label: 'Computing',
      icon: icons.computing,
      children: [
        { id: 'services', label: 'Services', icon: icons.services, path: '/services' },
        { id: 'managed-databases', label: 'Managed Databases', icon: icons.databases, path: '/managed-databases' },
        { id: 'managed-apps', label: 'Managed Apps', icon: icons.apps, path: '/managed-apps' },
      ],
    },
    {
      id: 'networking',
      label: 'Networking',
      icon: icons.networking,
      children: [
        { id: 'virtual-networks', label: 'Virtual Networks', icon: icons.virtualNetworks, path: '/networking/virtual-networks' },
        { id: 'domains', label: 'Domains', icon: icons.domains, path: '/networking/domains' },
      ],
    },
    {
      id: 'security',
      label: 'Security and IAM',
      icon: icons.security,
      children: [
        { id: 'accounts', label: 'Accounts', icon: icons.accounts, path: '/security/accounts' },
        { id: 'groups', label: 'Groups', icon: icons.groups, path: '/security/groups' },
        { id: 'role-rules', label: 'Role Rules', icon: icons.roleRules, path: '/security/role-rules' },
      ],
    },
    {
      id: 'billing',
      label: 'Billing',
      icon: icons.billing,
      children: [
        { id: 'analytics', label: 'Analytics', icon: icons.analytics, path: '/billing/analytics' },
        { id: 'invoices', label: 'Invoices', icon: icons.invoices, path: '/billing/invoices' },
        { id: 'billing-management', label: 'Billing Management', icon: icons.billingManagement, path: '/billing/management' },
      ],
    },
  ];

  const renderMenuItem = (item: MenuItem, level: number = 0) => {
    const hasChildren = item.children && item.children.length > 0;
    const isExpanded = expandedMenus.has(item.id);
    const isActive = item.path && location.pathname === item.path;

    if (hasChildren) {
      return (
        <li key={item.id}>
          <button
            className={`menu-item has-children ${isExpanded ? 'expanded' : ''}`}
            onClick={() => toggleMenu(item.id)}
            title={item.label}
          >
            {item.icon}
            <span className="nav-label">{item.label}</span>
            {!isCollapsed && (
              <span className="chevron">
                {isExpanded ? icons.chevronDown : icons.chevronRight}
              </span>
            )}
          </button>
          {isExpanded && !isCollapsed && (
            <ul className="submenu">
              {item.children!.map((child) => renderMenuItem(child, level + 1))}
            </ul>
          )}
        </li>
      );
    }

    return (
      <li key={item.id}>
        <Link
          to={item.path!}
          className={`menu-item ${isActive ? 'active' : ''}`}
          title={item.label}
        >
          {item.icon}
          <span className="nav-label">{item.label}</span>
        </Link>
      </li>
    );
  };

  return (
    <div className="layout-container">
      <aside className={`sidebar ${isCollapsed ? 'collapsed' : ''}`}>
        <div className="sidebar-header">
          <h1>
            <span className="logo-icon">☁</span>
            <span className="logo-text">Cloudios</span>
          </h1>
          <p>{user?.realmName}</p>
        </div>

        <nav className="sidebar-nav">
          <ul>
            {menuItems.map((item) => renderMenuItem(item))}
          </ul>
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-footer-content">
            <div className="sidebar-footer-info">
              <p>{user?.email}</p>
              <p>{user?.role}</p>
            </div>
            <button onClick={logout}>
              {isCollapsed ? (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                  <polyline points="16 17 21 12 16 7" />
                  <line x1="21" y1="12" x2="9" y2="12" />
                </svg>
              ) : (
                <span>Logout</span>
              )}
            </button>
          </div>
        </div>

        <button
          className="sidebar-toggle"
          onClick={() => setIsCollapsed(!isCollapsed)}
          title={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          {isCollapsed ? (
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="9 18 15 12 9 6" />
            </svg>
          ) : (
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          )}
        </button>
        <button
          className={`sidebar-pin ${isPinned ? 'pinned' : ''}`}
          onClick={togglePin}
          title={isPinned ? 'Unpin sidebar' : 'Pin sidebar'}
        >
          {isPinned ? icons.pinFilled : icons.pin}
        </button>
      </aside>

      <main className="main-content">
        {location.pathname !== '/' && <Header />}
        {location.pathname !== '/' && <Breadcrumb />}
        {children}
      </main>
    </div>
  );
}
