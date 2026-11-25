import type { ReactNode } from 'react';
import { errorMessage } from '../api/errors';

export function Spinner() {
  return <span className="spinner" aria-hidden="true" />;
}

/** État de chargement annoncé poliment aux technologies d'assistance. */
export function LoadingState({ label = 'Chargement…' }: { label?: string }) {
  return (
    <div className="state state--loading" role="status">
      <Spinner />
      <span>{label}</span>
    </div>
  );
}

/** Erreur bloquante d'un bloc de données, avec relance. */
export function ErrorState({ error, onRetry, title = 'Impossible de charger les données.' }: { error: unknown; onRetry?: () => void; title?: string }) {
  return (
    <div className="state state--error" role="alert">
      <p className="state__title">{title}</p>
      <p>{errorMessage(error)}</p>
      {onRetry && (
        <button type="button" className="button button--secondary" onClick={onRetry}>
          Réessayer
        </button>
      )}
    </div>
  );
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="state state--empty">
      <p className="state__title">{title}</p>
      {children}
    </div>
  );
}

type AlertTone = 'success' | 'error' | 'warning' | 'info';

/** Message en ligne. `error` et `warning` sont annoncés immédiatement (role="alert"). */
export function Alert({ tone, title, children, id }: { tone: AlertTone; title?: string; children?: ReactNode; id?: string }) {
  const assertive = tone === 'error' || tone === 'warning';
  return (
    <div id={id} className={`alert alert--${tone}`} role={assertive ? 'alert' : 'status'}>
      {title && <p className="alert__title">{title}</p>}
      {children}
    </div>
  );
}
