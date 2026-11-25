import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useAuth } from '../auth/AuthContext';
import { ErrorState, LoadingState } from './Feedback';

/** Écrans authentifiés : sans session → connexion ; mot de passe temporaire → changement obligatoire (GAP-06). */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { session, me, meError, isLoadingMe, refetchMe } = useAuth();
  const location = useLocation();

  if (!session) {
    return <Navigate to="/connexion" replace state={{ from: location.pathname }} />;
  }
  if (session.mustChangePassword || me?.mustChangePassword) {
    return <Navigate to="/mot-de-passe" replace />;
  }
  if (isLoadingMe) {
    return (
      <div className="page-center">
        <LoadingState label="Chargement de votre profil…" />
      </div>
    );
  }
  if (meError || !me) {
    return (
      <div className="page-center">
        <ErrorState error={meError} title="Impossible de charger votre profil." onRetry={refetchMe} />
      </div>
    );
  }
  return <>{children}</>;
}

/** Écran réservé à certains rôles. Le contrôle réel reste côté serveur (ENF-I2) : ceci n'est qu'ergonomique. */
export function RequireDecider({ children }: { children: ReactNode }) {
  const { canDecide } = useAuth();
  if (!canDecide) {
    return <Navigate to="/demandes/nouvelle" replace />;
  }
  return <>{children}</>;
}
