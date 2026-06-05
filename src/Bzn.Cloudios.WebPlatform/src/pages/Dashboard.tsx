import { Layout } from '../components/Layout';

export function Dashboard() {
  return (
    <Layout>
      <div className="dashboard">
        <h1>Global Dashboard</h1>
        
        <div className="dashboard-cards">
          <div className="dashboard-card">
            <h3>Total Realms</h3>
            <p className="blue">0</p>
          </div>
          <div className="dashboard-card">
            <h3>Active Containers</h3>
            <p className="green">0</p>
          </div>
          <div className="dashboard-card">
            <h3>Monthly Revenue</h3>
            <p className="purple">R$ 0,00</p>
          </div>
          <div className="dashboard-card">
            <h3>System Status</h3>
            <p className="green">Healthy</p>
          </div>
        </div>

        <div className="dashboard-section">
          <h2>Platform Overview</h2>
          <p>No data to display yet.</p>
        </div>
      </div>
    </Layout>
  );
}
