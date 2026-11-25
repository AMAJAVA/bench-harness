import type { TokenResponse } from '../api/types';
import { parseUtc } from '../lib/dates';

// Jeton en sessionStorage (step2_arch §2.4) : portée limitée à l'onglet, effacé à sa fermeture.
// Pas de jeton de rafraîchissement (ADR-02) : à l'expiration, l'utilisateur se reconnecte.

const STORAGE_KEY = 'teamleave.session';

export interface Session {
  accessToken: string;
  expiresAtMs: number;
  mustChangePassword: boolean;
}

export function sessionFromToken(token: TokenResponse): Session {
  return {
    accessToken: token.accessToken,
    expiresAtMs: parseUtc(token.expiresAtUtc).getTime(),
    mustChangePassword: token.mustChangePassword,
  };
}

export function isExpired(session: Session, now = Date.now()): boolean {
  return !Number.isFinite(session.expiresAtMs) || session.expiresAtMs <= now;
}

export function loadSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as Partial<Session>;
    if (typeof parsed.accessToken !== 'string' || typeof parsed.expiresAtMs !== 'number') {
      clearSession();
      return null;
    }
    const session: Session = {
      accessToken: parsed.accessToken,
      expiresAtMs: parsed.expiresAtMs,
      mustChangePassword: parsed.mustChangePassword === true,
    };
    if (isExpired(session)) {
      clearSession();
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

export function saveSession(session: Session): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearSession(): void {
  sessionStorage.removeItem(STORAGE_KEY);
}
