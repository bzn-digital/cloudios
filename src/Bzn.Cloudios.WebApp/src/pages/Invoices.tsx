import { Layout } from '../components/Layout';

const Invoices = () => {
  return (
    <Layout>
      <div className="services">
        <div className="services-header">
          <h1>Invoices</h1>
        </div>

        <div className="services-filters">
          <input
            type="text"
            placeholder="Search invoices..."
            className="search-input"
          />
          <select className="status-filter">
            <option value="All">All Status</option>
            <option value="Paid">Paid</option>
            <option value="Pending">Pending</option>
            <option value="Overdue">Overdue</option>
          </select>
        </div>

        <div className="services-table">
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Invoice #</th>
                <th>Date</th>
                <th>Amount</th>
                <th>Due Date</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td colSpan={6}>
                  <p className="empty-state">No invoices found.</p>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </Layout>
  );
};

export default Invoices;
