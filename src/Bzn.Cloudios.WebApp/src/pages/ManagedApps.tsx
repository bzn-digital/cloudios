import { Layout } from '../components/Layout';

const ManagedApps = () => {
  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Managed Apps</h1>
          <button className="btn btn-primary">+ Deploy App</button>
        </div>

        <div className="services-filters">
          <input
            type="text"
            placeholder="Search apps..."
            className="search-input"
          />
          <select className="status-filter">
            <option value="All">All Status</option>
            <option value="Running">Running</option>
            <option value="Stopped">Stopped</option>
            <option value="Failed">Failed</option>
          </select>
        </div>

        <div className="services-table">
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Name</th>
                <th>Type</th>
                <th>Region</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td colSpan={6}>
                  <p className="empty-state">No managed apps deployed yet.</p>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </Layout>
  );
};

export default ManagedApps;
