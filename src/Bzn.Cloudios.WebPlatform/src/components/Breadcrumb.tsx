import { Link, useLocation } from 'react-router-dom';

export function Breadcrumb() {
  const location = useLocation();
  const pathnames = location.pathname.split('/').filter((x) => x);

  const breadcrumbMap: Record<string, string> = {
    dashboard: 'Dashboard',
    computing: 'Computing',
    containers: 'Containers',
    servers: 'Servers',
    'services-templates': 'Services Templates',
    realms: 'Realms',
    settings: 'Settings',
  };

  const formatBreadcrumbName = (name: string) => {
    return breadcrumbMap[name] || name.charAt(0).toUpperCase() + name.slice(1);
  };

  if (pathnames.length === 0) {
    return null;
  }

  return (
    <nav className="breadcrumb">
      <Link to="/" className="breadcrumb-link">
        Home
      </Link>
      {pathnames.map((name, index) => {
        const routeTo = `/${pathnames.slice(0, index + 1).join('/')}`;
        const isLast = index === pathnames.length - 1;

        return (
          <div key={name} className="breadcrumb-item">
            <span className="breadcrumb-separator">/</span>
            {isLast ? (
              <span className="breadcrumb-current">{formatBreadcrumbName(name)}</span>
            ) : (
              <Link to={routeTo} className="breadcrumb-link">
                {formatBreadcrumbName(name)}
              </Link>
            )}
          </div>
        );
      })}
    </nav>
  );
}
