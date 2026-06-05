import { Layout } from '../components/Layout';

const Analytics = () => {
  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Analytics</h1>
        </div>

        <div className="dashboard-cards">
          <div className="dashboard-card">
            <h3>Total Cost (Month)</h3>
            <p className="card-value">R$ 0.00</p>
          </div>
          <div className="dashboard-card">
            <h3>Active Services</h3>
            <p className="card-value">0</p>
          </div>
          <div className="dashboard-card">
            <h3>Total Resources</h3>
            <p className="card-value">0</p>
          </div>
          <div className="dashboard-card">
            <h3>API Calls</h3>
            <p className="card-value">0</p>
          </div>
        </div>

        <div className="dashboard-section">
          <h2>Usage Overview</h2>
          <p className="empty-state">No analytics data available yet.</p>
        </div>
      </div>
    </Layout>
  );
};

export default Analytics;
