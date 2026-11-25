import type { ProblemDetails } from './types';

const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? '/api/v1').replace(/\/+$/, '');

/** Erreur d'appel API, construite à partir du Problem Details renvoyé par le serveur. */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly problem: ProblemDetails | null;
  readonly fieldErrors: Record<string, string[]>;
  readonly retryAfterSeconds: number | null;

  constructor(status: number, code: string, message: string, problem: ProblemDetails | null, retryAfterSeconds: number | null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.problem = problem;
    this.fieldErrors = problem?.errors ?? {};
    this.retryAfterSeconds = retryAfterSeconds;
  }
}

export const NETWORK_ERROR = 'NETWORK_ERROR';

export type AuthFailure = 'session-expired' | 'password-change-required';

interface ClientHooks {
  getToken: () => string | null;
  onAuthFailure: (failure: AuthFailure) => void;
}

let hooks: ClientHooks = { getToken: () => null, onAuthFailure: () => undefined };

/** Branché par AuthProvider : fournit le jeton et réagit aux 401 / 403 PASSWORD_CHANGE_REQUIRED. */
export function configureApiClient(next: ClientHooks): void {
  hooks = next;
}

type QueryValue = string | number | boolean | null | undefined;

export interface RequestOptions {
  body?: unknown;
  query?: Record<string, QueryValue>;
  signal?: AbortSignal;
  /** Appel sans jeton, dont le 401 ne signifie pas « session expirée » (POST /auth/login). */
  anonymous?: boolean;
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== null && value !== '') {
      params.set(key, String(value));
    }
  }
  const qs = params.toString();
  return `${API_BASE}${path}${qs ? `?${qs}` : ''}`;
}

function fallbackCode(status: number): string {
  switch (status) {
    case 400: return 'VALIDATION_ERROR';
    case 401: return 'UNAUTHENTICATED';
    case 403: return 'FORBIDDEN';
    case 404: return 'NOT_FOUND';
    case 409: return 'CONCURRENCY_CONFLICT';
    default: return status >= 500 ? 'SERVER_ERROR' : 'UNKNOWN_ERROR';
  }
}

export async function apiRequest<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }
  const token = options.anonymous ? null : hooks.getToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  let response: Response;
  try {
    response = await fetch(buildUrl(path, options.query), {
      method,
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: options.signal,
      credentials: 'same-origin',
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error;
    }
    throw new ApiError(0, NETWORK_ERROR, 'Le serveur est injoignable. Vérifiez votre connexion puis réessayez.', null, null);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const isJson = (response.headers.get('Content-Type') ?? '').includes('json');
  const payload: unknown = isJson ? await response.json().catch(() => null) : null;

  if (response.ok) {
    return payload as T;
  }

  const problem = (payload && typeof payload === 'object' ? payload : null) as ProblemDetails | null;
  const code = typeof problem?.code === 'string' ? problem.code : fallbackCode(response.status);
  const retryAfter = Number.parseInt(response.headers.get('Retry-After') ?? '', 10);
  const error = new ApiError(
    response.status,
    code,
    problem?.detail ?? problem?.title ?? `Erreur ${response.status}`,
    problem,
    Number.isFinite(retryAfter) ? retryAfter : null,
  );

  if (!options.anonymous) {
    if (response.status === 401) {
      hooks.onAuthFailure('session-expired');
    } else if (response.status === 403 && code === 'PASSWORD_CHANGE_REQUIRED') {
      hooks.onAuthFailure('password-change-required');
    }
  }

  throw error;
}
