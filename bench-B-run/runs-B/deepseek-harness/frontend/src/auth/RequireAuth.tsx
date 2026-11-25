import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { PasswordChangeGate } from './PasswordChangeGate';
import { FullPageLoader, PermissionNotice } from '../components/Feedback';

export function RequireAuth() {
  const { status, user } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <FullPageLoader label="Restauration de votre session…" />;
  }

  if (status === 'anonymous') {
    return (
      <Navigate
        to="/connexion"
        replace
        state={{ from: `${location.pathname}${location.search}` }}
      />
    );
  }

  if (user?.mustChangePassword) {
    return <PasswordChangeGate />;
  }

  return <Outlet />;
}

export function ManagerOnly({ children }: { children: ReactNode }) {
  const { user } = useAuth();

  if (!user?.roles.includes('MANAGER')) {
    return (
      <PermissionNotice title="Accès réservé aux managers">
        <p>
          La file de validation est accessible aux utilisateurs portant le rôle
          Manager. Vous pouvez néanmoins consulter le calendrier de votre équipe.
        </p>
      </PermissionNotice>
    );
  }

  return <>{children}</>;
}
