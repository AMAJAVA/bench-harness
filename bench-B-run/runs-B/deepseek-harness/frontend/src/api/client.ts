import type { ProblemDetails } from './types';

const API_BASE = (import.meta.env.VITE_API_BASE_URL || '/api/v1').replace(/\/+$/, '');

let accessToken: string | null = null;
let refreshInFlight: Promise<boolean> | null = null;
let sessionExpiredHandler: (() => void) | null = null;

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly problem: ProblemDetails;
  readonly retryAfter?: number;

  constructor(status: number, problem: ProblemDetails = {}, retryAfter?: number) {
    super(problem.detail || problem.title || `Erreur HTTP ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.code = problem.code;
    this.problem = problem;
    this.retryAfter = retryAfter;
  }
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}

export function setSessionExpiredHandler(handler: (() => void) | null): void {
  sessionExpiredHandler = handler;
}

function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return `web-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) {
    return {
      title: `Erreur HTTP ${response.status}`,
      status: response.status,
      detail: "Le serveur n'a pas renvoyé de détail exploitable.",
    };
  }

  try {
    const parsed: unknown = await response.json();
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      return parsed as ProblemDetails;
    }
  } catch {
    // Corps illisible : on retourne un problème générique.
  }

  return {
    title: `Erreur HTTP ${response.status}`,
    status: response.status,
    detail: 'La réponse du serveur est illisible.',
  };
}

interface SendOptions {
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  authenticated: boolean;
  signal?: AbortSignal;
}

async function send(path: string, options: SendOptions): Promise<Response> {
  const headers: Record<string, string> = {
    Accept: 'application/json, application/problem+json',
    'X-Correlation-Id': newCorrelationId(),
  };

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (options.authenticated && accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  try {
    return await fetch(`${API_BASE}${path}`, {
      method: options.method,
      headers,
      credentials: 'include',
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: options.signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error;
    }

    throw new ApiError(0, {
      title: 'Serveur injoignable',
      status: 0,
      code: 'NETWORK_ERROR',
      detail:
        "Impossible de joindre l'API TeamLeave. Vérifiez votre connexion réseau puis réessayez.",
    });
  }
}

async function parseSuccess<T>(response: Response): Promise<T> {
  if (response.status === 204 || response.status === 205) {
    return undefined as T;
  }

  if (response.status === 202) {
    return undefined as T;
  }

  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/**
 * Rafraîchit la session à partir du cookie `HttpOnly` posé par l'API.
 * Les appels concurrents partagent la même requête (single-flight) : un seul
 * refresh est émis, les autres attendent son résultat.
 */
export async function refreshAccessToken(): Promise<boolean> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        const response = await send('/auth/refresh', {
          method: 'POST',
          authenticated: false,
        });

        if (!response.ok) {
          accessToken = null;
          return false;
        }

        const payload = (await response.json()) as { accessToken?: string };
        if (!payload.accessToken) {
          accessToken = null;
          return false;
        }

        accessToken = payload.accessToken;
        return true;
      } catch {
        accessToken = null;
        return false;
      } finally {
        refreshInFlight = null;
      }
    })();
  }

  return refreshInFlight;
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  /** false pour les endpoints publics (login). */
  authenticated?: boolean;
  /** Désactive la tentative de refresh suivie d'un rejeu. */
  retryOnUnauthorized?: boolean;
  signal?: AbortSignal;
}

/**
 * Appelle l'API et normalise les erreurs en `ApiError`.
 *
 * Sur 401, une tentative de refresh unique est faite puis la requête est
 * rejouée. Si le refresh échoue, la session est considérée expirée.
 */
export async function apiRequest<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const method = options.method ?? 'GET';
  const authenticated = options.authenticated ?? true;
  let response = await send(path, {
    method,
    body: options.body,
    authenticated,
    signal: options.signal,
  });

  if (
    response.status === 401 &&
    authenticated &&
    options.retryOnUnauthorized !== false
  ) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      response = await send(path, {
        method,
        body: options.body,
        authenticated,
        signal: options.signal,
      });
    } else {
      sessionExpiredHandler?.();
    }
  }

  if (!response.ok) {
    const problem = await readProblem(response);
    const retryAfter = Number(response.headers.get('retry-after') ?? '');
    throw new ApiError(
      response.status,
      problem,
      Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : undefined,
    );
  }

  return parseSuccess<T>(response);
}

export function buildQuery(
  params: Record<string, string | number | boolean | undefined | null>,
): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  }
  const query = search.toString();
  return query ? `?${query}` : '';
}
