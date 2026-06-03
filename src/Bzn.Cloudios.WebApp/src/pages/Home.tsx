import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

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
      id: 'billing',
      title: 'Billing',
      description: 'View invoices and manage payment methods',
      icon: (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <line x1="12" y1="1" x2="12" y2="23" />
          <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
        </svg>
      ),
      path: '/billing',
      section: 'Billing',
    },
  ];

  const sections = ['Computing', 'Billing'] as const;

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
  );
};

export default Home;
