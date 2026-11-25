import { Alert } from './Alert';
import { ApiError } from '../api/client';

export function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 0) return error.message;
    if (error.status === 401) return 'Votre session a expiré. Merci de vous reconnecter.';
    if (error.status === 403) return "Vous n'avez pas les droits pour effectuer cette action.";
    if (error.status === 404) return 'La ressource demandée est introuvable.';
    if (error.status === 409) return error.message || 'Conflit : la ressource a été modifiée entre-temps.';
    if (error.status === 422) return error.message || 'La demande ne respecte pas les règles métier.';
    if (error.status >= 500) return 'Le service est momentanément indisponible. Réessayez plus tard.';
    return error.message || 'Une erreur est survenue.';
  }
  if (error instanceof Error) return error.message;
  return 'Une erreur inattendue est survenue.';
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div className="error-state">
      <Alert tone="error" title="Une erreur est survenue">
        {describeError(error)}
      </Alert>
      {onRetry && (
        <div className="row">
          <button type="button" className="btn btn-secondary" onClick={onRetry}>
            Réessayer
          </button>
        </div>
      )}
    </div>
  );
}
