import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/Layout';

interface QuickMenuItem {
  id: string;
  title: string;
  description: string;
  icon: React.ReactNode;
  path: string;
  section: string;
}

const Home = () => {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');

  const quickMenuItems: QuickMenuItem[] = [
    {
      id: 'services',
      title: 'Services',
      description: 'Manage and deploy container services',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M22 12h-4l-3 9L9 3l-3 9H2" />
        </svg>
      ),
      path: '/services',
      section: 'Computing',
    },
    {
      id: 'managed-databases',
      title: 'Managed Databases',
      description: 'Deploy and manage managed databases',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <ellipse cx="12" cy="5" rx="9" ry="3" />
          <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3" />
          <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5" />
        </svg>
      ),
      path: '/managed-databases',
      section: 'Computing',
    },
    {
      id: 'managed-apps',
      title: 'Managed Apps',
      description: 'Deploy and manage managed applications',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
          <line x1="8" y1="21" x2="16" y2="21" />
          <line x1="12" y1="17" x2="12" y2="21" />
        </svg>
      ),
      path: '/managed-apps',
      section: 'Computing',
    },
    {
      id: 'virtual-networks',
      title: 'Virtual Networks',
      description: 'Manage virtual networks and connectivity',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M20 17.58A5 5 0 0 0 18 8h-1.26A8 8 0 1 0 4 16.25" />
          <line x1="8" y1="16" x2="8.01" y2="16" />
          <line x1="8" y1="20" x2="8.01" y2="20" />
          <line x1="12" y1="18" x2="12.01" y2="18" />
          <line x1="12" y1="22" x2="12.01" y2="22" />
          <line x1="16" y1="16" x2="16.01" y2="16" />
          <line x1="16" y1="20" x2="16.01" y2="20" />
        </svg>
      ),
      path: '/networking/virtual-networks',
      section: 'Networking',
    },
    {
      id: 'domains',
      title: 'Domains',
      description: 'Manage custom domains and DNS',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="10" />
          <line x1="2" y1="12" x2="22" y2="12" />
          <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
        </svg>
      ),
      path: '/networking/domains',
      section: 'Networking',
    },
    {
      id: 'analytics',
      title: 'Analytics',
      description: 'View usage analytics and insights',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <line x1="18" y1="20" x2="18" y2="10" />
          <line x1="12" y1="20" x2="12" y2="4" />
          <line x1="6" y1="20" x2="6" y2="14" />
        </svg>
      ),
      path: '/billing/analytics',
      section: 'Billing',
    },
    {
      id: 'invoices',
      title: 'Invoices',
      description: 'View and download invoices',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
          <polyline points="14 2 14 8 20 8" />
          <line x1="16" y1="13" x2="8" y2="13" />
          <line x1="16" y1="17" x2="8" y2="17" />
          <polyline points="10 9 9 9 8 9" />
        </svg>
      ),
      path: '/billing/invoices',
      section: 'Billing',
    },
    {
      id: 'billing-management',
      title: 'Billing Management',
      description: 'Manage payment methods and billing settings',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <rect x="1" y="4" width="22" height="16" rx="2" ry="2" />
          <line x1="1" y1="10" x2="23" y2="10" />
        </svg>
      ),
      path: '/billing/management',
      section: 'Billing',
    },
  ];

  const sections = ['Computing', 'Networking', 'Billing'] as const;

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      // For now, just navigate to services with search query
      navigate(`/services?search=${encodeURIComponent(searchQuery)}`);
    }
  };

  const handleQuickMenuClick = (item: QuickMenuItem) => {
    navigate(item.path);
  };

  return (
    <Layout>
      <div className="home">
        <div className="home-header">
          <h1>Welcome to Cloudios</h1>
          <p>Manage your cloud infrastructure with ease</p>
        </div>

        <div className="home-search">
          <form onSubmit={handleSearch}>
            <div className="search-wrapper">
              <svg className="search-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="11" cy="11" r="8" />
                <line x1="21" y1="21" x2="16.65" y2="16.65" />
              </svg>
              <input
                type="text"
                placeholder="Search services, pages, or resources..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="search-input"
              />
            </div>
          </form>
        </div>

        <div className="home-content">
          {sections.map((section) => (
            <div key={section} className="home-section">
              <h2 className="section-title">{section}</h2>
              <div className="quick-menu-grid">
                {quickMenuItems
                  .filter((item) => item.section === section)
                  .map((item) => (
                    <button
                      key={item.id}
                      onClick={() => handleQuickMenuClick(item)}
                      className="quick-menu-item"
                    >
                      <div className="quick-menu-icon">{item.icon}</div>
                      <div className="quick-menu-content">
                        <h3>{item.title}</h3>
                        <p>{item.description}</p>
                      </div>
                      <svg className="quick-menu-arrow" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <line x1="5" y1="12" x2="19" y2="12" />
                        <polyline points="12 5 19 12 12 19" />
                      </svg>
                    </button>
                  ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </Layout>
  );
};

export default Home;
