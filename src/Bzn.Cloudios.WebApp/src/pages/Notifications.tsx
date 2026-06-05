import { useState } from 'react';
import { Layout } from '../components/Layout';

interface Notification {
  id: string;
  type: 'error' | 'warning' | 'info' | 'success';
  title: string;
  message: string;
  time: string;
  read: boolean;
}

const Notifications = () => {
  const [filter, setFilter] = useState<'all' | 'unread' | 'error' | 'warning' | 'info' | 'success'>('all');
  const [searchQuery, setSearchQuery] = useState('');

  const notifications: Notification[] = [
    {
      id: '1',
      type: 'error',
      title: 'Service Stopped',
      message: 'Service "my-app" has stopped unexpectedly',
      time: '2 minutes ago',
      read: false,
    },
    {
      id: '2',
      type: 'warning',
      title: 'High CPU Usage',
      message: 'Service "api-server" is using 95% CPU',
      time: '15 minutes ago',
      read: false,
    },
    {
      id: '3',
      type: 'info',
      title: 'Deployment Complete',
      message: 'Service "web-app" has been deployed successfully',
      time: '1 hour ago',
      read: true,
    },
    {
      id: '4',
      type: 'success',
      title: 'Domain Renewed',
      message: 'Domain "example.com" has been renewed for 1 year',
      time: '2 hours ago',
      read: true,
    },
    {
      id: '5',
      type: 'info',
      title: 'Backup Completed',
      message: 'Database backup completed successfully',
      time: '3 hours ago',
      read: true,
    },
  ];

  const getNotificationIcon = (type: Notification['type']) => {
    switch (type) {
      case 'error':
        return (
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
        );
      case 'warning':
        return (
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
            <line x1="12" y1="9" x2="12" y2="13" />
            <line x1="12" y1="17" x2="12.01" y2="17" />
          </svg>
        );
      case 'success':
        return (
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
            <polyline points="22 4 12 14.01 9 11.01" />
          </svg>
        );
      default:
        return (
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="16" x2="12" y2="12" />
            <line x1="12" y1="8" x2="12.01" y2="8" />
          </svg>
        );
    }
  };

  const getNotificationColor = (type: Notification['type']) => {
    switch (type) {
      case 'error':
        return 'var(--bzn-error)';
      case 'warning':
        return 'var(--bzn-warning)';
      case 'success':
        return 'var(--bzn-success)';
      default:
        return 'var(--bzn-primary)';
    }
  };

  const filteredNotifications = notifications.filter((notification) => {
    const matchesFilter = filter === 'all' || 
      (filter === 'unread' && !notification.read) ||
      (filter === notification.type);
    const matchesSearch = notification.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      notification.message.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesFilter && matchesSearch;
  });

  const markAsRead = (id: string) => {
    // In a real app, this would update the backend
    console.log('Mark as read:', id);
  };

  const markAllAsRead = () => {
    // In a real app, this would update the backend
    console.log('Mark all as read');
  };

  return (
    <Layout>
      <div className="notifications">
        <div className="notifications-header">
          <h1>Notifications</h1>
          <button className="btn btn-secondary" onClick={markAllAsRead}>
            Mark All as Read
          </button>
        </div>

        <div className="notifications-filters">
          <input
            type="text"
            placeholder="Search notifications..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="search-input"
          />
          <select
            value={filter}
            onChange={(e) => setFilter(e.target.value as any)}
            className="status-filter"
          >
            <option value="all">All</option>
            <option value="unread">Unread</option>
            <option value="error">Error</option>
            <option value="warning">Warning</option>
            <option value="info">Info</option>
            <option value="success">Success</option>
          </select>
        </div>

        <div className="notifications-list">
          {filteredNotifications.length === 0 ? (
            <p className="empty-state">No notifications found.</p>
          ) : (
            filteredNotifications.map((notification) => (
              <div
                key={notification.id}
                className={`notification-item ${notification.read ? 'read' : 'unread'}`}
                onClick={() => markAsRead(notification.id)}
              >
                <div
                  className="notification-icon"
                  style={{ color: getNotificationColor(notification.type) }}
                >
                  {getNotificationIcon(notification.type)}
                </div>
                <div className="notification-content">
                  <div className="notification-header">
                    <h3 className="notification-title">{notification.title}</h3>
                    <span className="notification-time">{notification.time}</span>
                  </div>
                  <p className="notification-message">{notification.message}</p>
                </div>
                {!notification.read && <div className="notification-dot" />}
              </div>
            ))
          )}
        </div>
      </div>
    </Layout>
  );
};

export default Notifications;
