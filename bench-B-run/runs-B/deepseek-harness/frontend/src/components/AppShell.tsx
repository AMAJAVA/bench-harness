import { useState } from 'react';
import { Link, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { fullName, initials } from '../lib/dates';
import { roleLabel } from '../lib/labels';
import { CalendarIcon, InboxIcon, LogOutIcon, SendIcon } from './icons';
import { Spinner } from './Feedback';

const NAV_ITEMS = [
  {
    to: '/demandes/nouvelle',
    label: 'Nouvelle demande',
    shortLabel: 'Demande',
    icon: SendIcon,
    managerOnly: false,
  },
  {
    to: '/calendrier',
    label: 'Calendrier d’équipe',
    shortLabel: 'Calendrier',
    icon: CalendarIcon,
    managerOnly: false,
  },
  {
    to: '/validation',
    label: 'File de validation',
    shortLabel: 'Validation',
    icon: InboxIcon,
    managerOnly: true,
  },
] as const;

export function AppShell() {
  const { user, logout } = useAuth();
  const [loggingOut, setLoggingOut] = useState(false);

  if (!user) {
    return null;
  }

  const isManager = user.roles.includes('MANAGER');

  const handleLogout = async () => {
    setLoggingOut(true);
    try {
      await logout();
    } finally {
      setLoggingOut(false);
    }
  };

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="app-header-inner">
          <Link to="/demandes/nouvelle" className="brand" aria-label="TeamLeave, accueil">
            <span className="brand-mark" aria-hidden="true">
              TL
            </span>
            <span className="brand-text">
              <span className="brand-name">TeamLeave</span>
              <span className="brand-tagline">Gestion des congés</span>
            </span>
          </Link>

          <nav className="main-nav" aria-label="Navigation principale">
            <ul className="nav-list">
              {NAV_ITEMS.filter((item) => !item.managerOnly || isManager).map(
                (item) => {
                  const Icon = item.icon;
                  return (
                    <li key={item.to}>
                      <NavLink
                        to={item.to}
                        className={({ isActive }) =>
                          `nav-link${isActive ? ' nav-link-active' : ''}`
                        }
                      >
                        <Icon size={18} />
                        <span className="nav-label">{item.label}</span>
                        <span className="nav-label-short">{item.shortLabel}</span>
                      </NavLink>
                    </li>
                  );
                },
              )}
            </ul>
          </nav>

          <div className="user-area">
            <div className="user-identity">
              <span className="avatar" aria-hidden="true">
                {initials(user.firstName, user.lastName)}
              </span>
              <span className="user-text">
                <span className="user-name">{fullName(user)}</span>
                <span className="user-meta">
                  {roleLabel(user.roles)}
                  {user.team ? ` · ${user.team.name}` : ''}
                </span>
              </span>
            </div>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={handleLogout}
              disabled={loggingOut}
              aria-label={loggingOut ? 'Déconnexion en cours' : 'Se déconnecter'}
            >
              {loggingOut ? <Spinner /> : <LogOutIcon size={18} />}
              <span>{loggingOut ? 'Déconnexion…' : 'Se déconnecter'}</span>
            </button>
          </div>
        </div>
      </header>

      <main id="main-content" className="app-main" tabIndex={-1}>
        <Outlet />
      </main>

      <footer className="app-footer">
        <p>TeamLeave · Application interne de gestion des congés</p>
        <p className="app-footer-note">
          Absences approuvées uniquement · Données Europe/Paris
        </p>
      </footer>
    </div>
  );
}
