import { useState, useEffect } from 'react';
import { Layout } from '../components/Layout';
import { apiClient } from '../lib/api';
import type { RealmBillingResponse, MetricDataPoint } from '../types/billing';
import type { ContainerListResponse, ContainerListItem } from '../types/container';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

export function Dashboard() {
  const [billing, setBilling] = useState<RealmBillingResponse | null>(null);
  const [containers, setContainers] = useState<ContainerListItem[]>([]);
  const [metrics, setMetrics] = useState<MetricDataPoint[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      setLoading(true);
      const now = new Date();
      const year = now.getFullYear();
      const month = now.getMonth() + 1;

      const from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0];
      const to = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().split('T')[0];

      const [billingData, containersData, metricsData] = await Promise.all([
        apiClient.getRealmBilling(year, month) as Promise<RealmBillingResponse>,
        apiClient.getContainers(undefined, undefined, 1, 100) as Promise<ContainerListResponse>,
        apiClient.getRealmMetricsHistory(from, to) as unknown as { dataPoints: MetricDataPoint[] },
      ]);

      setBilling(billingData);
      setContainers(containersData.items || []);
      setMetrics(metricsData.dataPoints || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load dashboard data');
    } finally {
      setLoading(false);
    }
  };

  const runningCount = containers.filter(c => c.status === 'Running').length;
  const stoppedCount = containers.filter(c => c.status === 'Stopped' || c.status === 'Failed').length;

  const chartData = metrics.map(m => ({
    time: new Date(m.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    cpu: m.cpuPercent.toFixed(1),
    memory: (m.memoryUsedBytes / (1024 * 1024)).toFixed(2),
  }));

  if (loading) {
    return (
      <Layout>
        <div className="dashboard">
          <h1>Dashboard</h1>
          <p>Loading...</p>
        </div>
      </Layout>
    );
  }

  if (error) {
    return (
      <Layout>
        <div className="dashboard">
          <h1>Dashboard</h1>
          <p className="error">{error}</p>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="dashboard">
        <h1>Dashboard</h1>
        
        <div className="dashboard-cards">
          <div className="dashboard-card">
            <h3>Running Services</h3>
            <p className="green">{runningCount}</p>
          </div>
          <div className="dashboard-card">
            <h3>Stopped Services</h3>
            <p className="yellow">{stoppedCount}</p>
          </div>
          <div className="dashboard-card">
            <h3>Total Cost (Month)</h3>
            <p className="blue">
              R$ {billing?.totalCostBRL?.toFixed(2) || '0,00'}
            </p>
          </div>
        </div>

        <div className="dashboard-section">
          <h2>Resource Usage (Last 24h)</h2>
          {chartData.length === 0 ? (
            <p>No metrics data available.</p>
          ) : (
            <div className="metrics-chart">
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="time" />
                  <YAxis yAxisId="cpu" orientation="left" />
                  <YAxis yAxisId="memory" orientation="right" />
                  <Tooltip />
                  <Line 
                    yAxisId="cpu" 
                    type="monotone" 
                    dataKey="cpu" 
                    stroke="#9333ea" 
                    name="CPU %"
                    strokeWidth={2}
                    dot={{ fill: '#9333ea', r: 3 }}
                  />
                  <Line 
                    yAxisId="memory" 
                    type="monotone" 
                    dataKey="memory" 
                    stroke="#f97316" 
                    name="Memory (MB)"
                    strokeWidth={2}
                    dot={{ fill: '#f97316', r: 3 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>

        <div className="dashboard-section">
          <h2>Services Overview</h2>
          {containers.length === 0 ? (
            <p>No services deployed yet.</p>
          ) : (
            <div className="dashboard-services">
              {containers.map(container => (
                <div key={container.id} className="dashboard-service-item">
                  <div className="service-info">
                    <h4>{container.name}</h4>
                    <p>{container.imageName}</p>
                  </div>
                  <div className="service-status">
                    <span className={`status-badge status-${container.status.toLowerCase()}`}>
                      {container.status}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </Layout>
  );
}
