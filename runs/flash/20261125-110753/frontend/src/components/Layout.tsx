import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Layout() {
  const { user, isManager, logout } = useAuth();
  const navigate = useNavigate();
  const initials = user
    ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
    : '?';

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">Aller au contenu</a>
      <header className="app-header">
        <div className="app-header-inner">
          <Link to="/" className="brand" aria-label="TeamLeave, retour à l'accueil">
            <span className="brand-mark" aria-hidden="true">TL</span>
            <span>TeamLeave</span>
          </Link>
          <nav aria-label="Navigation principale">
            <ul className="nav-list">
              <li>
                <NavLink to="/requests/new" className="nav-link">
                  Nouvelle demande
                </NavLink>
              </li>
              <li>
                <NavLink to="/calendar" className="nav-link">
                  Calendrier d'équipe
                </NavLink>
              </li>
              {isManager && (
                <li>
                  <NavLink to="/approvals" className="nav-link">
                    Validation
                  </NavLink>
                </li>
              )}
            </ul>
          </nav>
          <div className="user-summary">
            <span className="avatar" aria-hidden="true">{initials}</span>
            <span>{user?.firstName} {user?.lastName}</span>
            <button type="button" className="btn btn-ghost btn-sm" onClick={handleLogout}>
              Se déconnecter
            </button>
          </div>
        </div>
      </header>
      <main id="main-content" className="app-main" tabIndex={-1}>
        <Outlet />
      </main>
    </div>
  );
}
