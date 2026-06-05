import { useState } from 'react';
import { Layout } from '../components/Layout';

const Domains = () => {
  const [activeTab, setActiveTab] = useState<'domains' | 'subdomains' | 'virtual-domains'>('domains');
  const [searchQuery, setSearchQuery] = useState('');
  const [showRegisterModal, setShowRegisterModal] = useState(false);

  const renderDomainsTab = () => (
    <div className="domains-content">
      <div className="domains-header">
        <h1>Domains</h1>
        <button className="btn btn-primary" onClick={() => setShowRegisterModal(true)}>
          + Register Domain
        </button>
      </div>

      <div className="domains-filters">
        <input
          type="text"
          placeholder="Search domains..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="search-input"
        />
      </div>

      <div className="domains-table">
        <table>
          <thead>
            <tr>
              <th>Domain</th>
              <th>Status</th>
              <th>Expires At</th>
              <th>Auto Renew</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={5}>
                <p className="empty-state">No domains found.</p>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );

  const renderSubdomainsTab = () => (
    <div className="domains-content">
      <div className="domains-header">
        <h1>Subdomains</h1>
        <button className="btn btn-primary">
          + Create Subdomain
        </button>
      </div>

      <div className="domains-filters">
        <input
          type="text"
          placeholder="Search subdomains..."
          className="search-input"
        />
      </div>

      <div className="domains-table">
        <table>
          <thead>
            <tr>
              <th>Subdomain</th>
              <th>Domain</th>
              <th>Status</th>
              <th>Created At</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={5}>
                <p className="empty-state">No subdomains found.</p>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );

  const renderVirtualDomainsTab = () => (
    <div className="domains-content">
      <div className="domains-header">
        <h1>Virtual Domains</h1>
        <button className="btn btn-primary">
          + Create Virtual Domain
        </button>
      </div>

      <div className="domains-filters">
        <input
          type="text"
          placeholder="Search virtual domains..."
          className="search-input"
        />
      </div>

      <div className="domains-table">
        <table>
          <thead>
            <tr>
              <th>Virtual Domain</th>
              <th>Status</th>
              <th>Target</th>
              <th>Created At</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>bzn-digital.com</td>
              <td><span className="status-badge active">Active</span></td>
              <td>-</td>
              <td>-</td>
              <td>-</td>
            </tr>
            <tr>
              <td>bzn.digital</td>
              <td><span className="status-badge active">Active</span></td>
              <td>-</td>
              <td>-</td>
              <td>-</td>
            </tr>
            <tr>
              <td>bzn.app</td>
              <td><span className="status-badge active">Active</span></td>
              <td>-</td>
              <td>-</td>
              <td>-</td>
            </tr>
            <tr>
              <td>bzn-cloud.com</td>
              <td><span className="status-badge active">Active</span></td>
              <td>-</td>
              <td>-</td>
              <td>-</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );

  return (
    <Layout>
      <div className="domains">
        <div className="domains-tabs">
          <button
            className={`tab-button ${activeTab === 'domains' ? 'active' : ''}`}
            onClick={() => setActiveTab('domains')}
          >
            Domains
          </button>
          <button
            className={`tab-button ${activeTab === 'subdomains' ? 'active' : ''}`}
            onClick={() => setActiveTab('subdomains')}
          >
            Subdomains
          </button>
          <button
            className={`tab-button ${activeTab === 'virtual-domains' ? 'active' : ''}`}
            onClick={() => setActiveTab('virtual-domains')}
          >
            Virtual Domains
          </button>
        </div>

        {activeTab === 'domains' && renderDomainsTab()}
        {activeTab === 'subdomains' && renderSubdomainsTab()}
        {activeTab === 'virtual-domains' && renderVirtualDomainsTab()}

        {showRegisterModal && (
          <div className="modal-overlay" onClick={() => setShowRegisterModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h2>Register Domain</h2>
                <button className="modal-close" onClick={() => setShowRegisterModal(false)}>×</button>
              </div>
              <div className="modal-body">
                <div className="form-group">
                  <label htmlFor="domain-search">Domain Name</label>
                  <input
                    id="domain-search"
                    type="text"
                    placeholder="example.com"
                    className="modal-input"
                  />
                </div>
                <div className="domain-availability">
                  <p className="availability-status">Check availability to see pricing</p>
                </div>
              </div>
              <div className="modal-footer">
                <button className="btn btn-secondary" onClick={() => setShowRegisterModal(false)}>
                  Cancel
                </button>
                <button className="btn btn-primary">
                  Check Availability
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
};

export default Domains;
