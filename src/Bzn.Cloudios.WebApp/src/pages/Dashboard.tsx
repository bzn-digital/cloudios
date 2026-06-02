import { Layout } from '../components/Layout';

export function Dashboard() {
  return (
    <Layout>
      <div className="dashboard">
        <h1>Dashboard</h1>
        
        <div className="dashboard-cards">
          <div className="dashboard-card">
            <h3>Active Services</h3>
            <p className="blue">0</p>
          </div>
          <div className="dashboard-card">
            <h3>Total Cost (Month)</h3>
            <p className="green">R$ 0,00</p>
          </div>
          <div className="dashboard-card">
            <h3>Team Members</h3>
            <p className="purple">0</p>
          </div>
        </div>

        <div className="dashboard-section">
          <h2>Recent Activity</h2>
          <p>No recent activity to display.</p>
        </div>
      </div>
    </Layout>
  );
}
