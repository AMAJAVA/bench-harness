import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { api } from '../api/endpoints';
import {
  refreshAccessToken,
  setAccessToken,
  setSessionExpiredHandler,
} from '../api/client';
import type { MeResponse } from '../api/types';

type AuthStatus = 'loading' | 'anonymous' | 'authenticated';

interface AuthContextValue {
  status: AuthStatus;
  user: MeResponse | null;
  sessionNotice: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  completePasswordChange: () => void;
  refreshUser: () => Promise<void>;
  clearSessionNotice: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [user, setUser] = useState<MeResponse | null>(null);
  const [sessionNotice, setSessionNotice] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const clearSession = useCallback(
    (notice: string | null) => {
      setAccessToken(null);
      setUser(null);
      setStatus('anonymous');
      setSessionNotice(notice);
      queryClient.clear();
    },
    [queryClient],
  );

  // Reprise de session : le jeton d'accès vit en mémoire, le jeton de
  // rafraîchissement est un cookie HttpOnly. Au chargement, on tente donc un
  // refresh silencieux avant de décider si l'utilisateur est authentifié.
  useEffect(() => {
    let cancelled = false;

    const restore = async () => {
      const refreshed = await refreshAccessToken();
      if (cancelled) {
        return;
      }

      if (!refreshed) {
        setStatus('anonymous');
        return;
      }

      try {
        const me = await api.getMe();
        if (!cancelled) {
          setUser(me);
          setStatus('authenticated');
        }
      } catch {
        if (!cancelled) {
          setAccessToken(null);
          setUser(null);
          setStatus('anonymous');
        }
      }
    };

    void restore();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    setSessionExpiredHandler(() => {
      clearSession('Votre session a expiré. Reconnectez-vous pour continuer.');
    });

    return () => setSessionExpiredHandler(null);
  }, [clearSession]);

  const login = useCallback(async (email: string, password: string) => {
    const response = await api.login(email, password);
    setAccessToken(response.accessToken);

    try {
      const me = await api.getMe();
      setUser(me);
      setStatus('authenticated');
      setSessionNotice(null);
    } catch (error) {
      setAccessToken(null);
      throw error;
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await api.logout();
    } catch {
      // Une déconnexion locale doit réussir même si le serveur est injoignable.
    }
    clearSession(null);
  }, [clearSession]);

  const completePasswordChange = useCallback(() => {
    // Le changement de mot de passe révoque tous les jetons côté serveur :
    // l'utilisateur doit se reconnecter avec son nouveau secret.
    clearSession('Votre mot de passe a été modifié. Connectez-vous avec celui-ci.');
  }, [clearSession]);

  const refreshUser = useCallback(async () => {
    const me = await api.getMe();
    setUser(me);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      sessionNotice,
      login,
      logout,
      completePasswordChange,
      refreshUser,
      clearSessionNotice: () => setSessionNotice(null),
    }),
    [
      status,
      user,
      sessionNotice,
      login,
      logout,
      completePasswordChange,
      refreshUser,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth doit être utilisé dans un AuthProvider.');
  }
  return context;
}
