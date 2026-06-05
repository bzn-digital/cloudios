import { Layout } from '../components/Layout';

const BillingManagement = () => {
  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Billing Management</h1>
          <button className="btn btn-primary">+ Add Payment Method</button>
        </div>

        <div className="dashboard-cards">
          <div className="dashboard-card">
            <h3>Default Payment Method</h3>
            <p className="card-value">Not configured</p>
          </div>
          <div className="dashboard-card">
            <h3>Billing Cycle</h3>
            <p className="card-value">Monthly</p>
          </div>
          <div className="dashboard-card">
            <h3>Payment Methods</h3>
            <p className="card-value">0</p>
          </div>
        </div>

        <div className="dashboard-section">
          <h2>Payment Methods</h2>
          <p className="empty-state">No payment methods configured yet.</p>
        </div>
      </div>
    </Layout>
  );
};

export default BillingManagement;
