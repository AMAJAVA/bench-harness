import { NavLink, Outlet } from 'react-router';
import { useAuth } from '../auth/AuthContext';
import { ROLE_LABELS } from '../lib/format';

export function Layout() {
  const { me, canDecide, logout } = useAuth();

  return (
    <div className="app">
      <a className="skip-link" href="#contenu">
        Aller au contenu
      </a>
      <header className="header">
        <div className="header__inner">
          <p className="brand">
            <span className="brand__mark" aria-hidden="true" />
            TeamLeave
          </p>
          <nav className="nav" aria-label="Navigation principale">
            <ul className="nav__list">
              <li>
                <NavLink className="nav__link" to="/demandes/nouvelle">
                  Nouvelle demande
                </NavLink>
              </li>
              <li>
                <NavLink className="nav__link" to="/calendrier">
                  Calendrier d'équipe
                </NavLink>
              </li>
              {canDecide && (
                <li>
                  <NavLink className="nav__link" to="/validation">
                    Demandes à valider
                  </NavLink>
                </li>
              )}
            </ul>
          </nav>
          <div className="user">
            {me && (
              <p className="user__name">
                {me.firstName} {me.lastName}
                <span className="user__roles">{me.roles.map((role) => ROLE_LABELS[role]).join(' · ')}</span>
              </p>
            )}
            <button type="button" className="button button--ghost" onClick={() => logout('Vous êtes déconnecté.')}>
              Se déconnecter
            </button>
          </div>
        </div>
      </header>
      <main id="contenu" className="main" tabIndex={-1}>
        <Outlet />
      </main>
    </div>
  );
}
