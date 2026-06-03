import { Layout } from '../components/Layout';

const ManagedApps = () => {
  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Managed Apps</h1>
          <button className="btn btn-primary">Deploy App</button>
        </div>

        <div className="services-filters">
          <input type="text" placeholder="Search apps..." className="form-control" />
          <select className="form-control">
            <option value="">All Status</option>
            <option value="running">Running</option>
            <option value="stopped">Stopped</option>
          </select>
        </div>

        <div className="services-table-container">
          <table className="services-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Type</th>
                <th>Region</th>
                <th>Status</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td colSpan={6} className="text-center">
                  <p className="text-muted">No managed apps deployed yet.</p>
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
