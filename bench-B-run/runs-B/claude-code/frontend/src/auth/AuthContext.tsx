import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { configureApiClient, type AuthFailure } from '../api/client';
import { api, queryKeys } from '../api/endpoints';
import type { Me, Role } from '../api/types';
import { clearSession, loadSession, saveSession, sessionFromToken, type Session } from './session';

interface AuthContextValue {
  session: Session | null;
  me: Me | undefined;
  meError: unknown;
  isLoadingMe: boolean;
  refetchMe: () => void;
  /** Message à afficher sur l'écran de connexion (session expirée, mot de passe changé…). */
  notice: string | null;
  login: (email: string, password: string) => Promise<Session>;
  logout: (notice?: string) => void;
  hasRole: (role: Role) => boolean;
  canDecide: boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// Limite de setTimeout (≈ 24,8 jours) : sans objet pour un jeton de 60 minutes, mais évite un déclenchement immédiat.
const MAX_TIMEOUT_MS = 2_147_483_647;

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [session, setSession] = useState<Session | null>(() => loadSession());
  const [notice, setNotice] = useState<string | null>(null);

  const sessionRef = useRef(session);
  sessionRef.current = session;

  const logout = useCallback(
    (message?: string) => {
      clearSession();
      sessionRef.current = null;
      setSession(null);
      setNotice(message ?? null);
      queryClient.clear();
    },
    [queryClient],
  );

  const handleAuthFailure = useCallback(
    (failure: AuthFailure) => {
      if (!sessionRef.current) {
        return;
      }
      if (failure === 'session-expired') {
        logout('Votre session a expiré. Reconnectez-vous pour continuer.');
      } else {
        const next = { ...sessionRef.current, mustChangePassword: true };
        saveSession(next);
        setSession(next);
      }
    },
    [logout],
  );

  const failureRef = useRef(handleAuthFailure);
  failureRef.current = handleAuthFailure;

  // Branché pendant le premier rendu : les requêtes des composants enfants partent avec le jeton dès le montage.
  useState(() =>
    configureApiClient({
      getToken: () => sessionRef.current?.accessToken ?? null,
      onAuthFailure: (failure) => failureRef.current(failure),
    }),
  );

  // Déconnexion à l'expiration du jeton (60 min, pas de rafraîchissement — ADR-02).
  useEffect(() => {
    if (!session) {
      return undefined;
    }
    const delay = Math.min(Math.max(session.expiresAtMs - Date.now(), 0), MAX_TIMEOUT_MS);
    const timer = window.setTimeout(() => logout('Votre session a expiré. Reconnectez-vous pour continuer.'), delay);
    return () => window.clearTimeout(timer);
  }, [session, logout]);

  const meQuery = useQuery({
    queryKey: queryKeys.me,
    queryFn: ({ signal }) => api.getMe(signal),
    enabled: session !== null,
    staleTime: 5 * 60_000,
  });

  const login = useCallback(
    async (email: string, password: string) => {
      const token = await api.login({ email, password });
      const next = sessionFromToken(token);
      queryClient.clear();
      saveSession(next);
      sessionRef.current = next;
      setSession(next);
      setNotice(null);
      return next;
    },
    [queryClient],
  );

  const me = session ? meQuery.data : undefined;
  const { refetch } = meQuery;
  const refetchMe = useCallback(() => void refetch(), [refetch]);

  const value = useMemo<AuthContextValue>(() => {
    const roles = me?.roles ?? [];
    return {
      session,
      me,
      meError: meQuery.error,
      isLoadingMe: session !== null && meQuery.isPending,
      refetchMe,
      notice,
      login,
      logout,
      hasRole: (role) => roles.includes(role),
      canDecide: roles.includes('Manager') || roles.includes('HrAdmin'),
    };
  }, [session, me, meQuery.error, meQuery.isPending, refetchMe, notice, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error('useAuth doit être utilisé sous <AuthProvider>.');
  }
  return value;
}
