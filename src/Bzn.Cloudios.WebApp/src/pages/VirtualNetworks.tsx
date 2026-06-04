import { Layout } from '../components/Layout';

const VirtualNetworks = () => {
  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Virtual Networks</h1>
          <button className="btn btn-primary">+ Create Network</button>
        </div>

        <div className="services-filters">
          <input
            type="text"
            placeholder="Search networks..."
            className="search-input"
          />
          <select className="status-filter">
            <option value="All">All Status</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
          </select>
        </div>

        <div className="services-table">
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Name</th>
                <th>CIDR</th>
                <th>Subnets</th>
                <th>Created At</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td colSpan={6}>
                  <p className="empty-state">No virtual networks found.</p>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </Layout>
  );
};

export default VirtualNetworks;
