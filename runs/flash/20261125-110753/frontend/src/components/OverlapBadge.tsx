import type { OverlapDetail } from '../api/types';
import { formatDate } from '../utils/dates';

export function OverlapBadge({
  hasWarning,
  ratio,
  details,
}: {
  hasWarning: boolean;
  ratio?: number | null;
  details?: OverlapDetail[] | null;
}) {
  if (!hasWarning) return null;
  const percent = ratio != null ? Math.round(ratio * 100) : null;

  return (
    <div className="alert alert-warning" role="alert">
      <span className="alert-icon" aria-hidden="true">!</span>
      <div>
        <div className="alert-title">
          Chevauchement critique{percent != null ? ` — ${percent} %` : ''}
        </div>
        <div className="alert-body">
          {details && details.length > 0 ? (
            <>
              <p style={{ margin: '0 0 0.25rem' }}>
                {details.length} jour{details.length > 1 ? 's' : ''} de cette demande{' '}
                {details.length > 1 ? 'sont' : 'est'} déjà couvert{details.length > 1 ? 's' : ''} par des absences approuvées :
              </p>
              <ul>
                {details.map((d) => (
                  <li key={d.date}>
                    {formatDate(d.date)} — {d.absentEmployees.join(', ')}
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <p style={{ margin: 0 }}>
              Cette demande chevauche plus de 50 % d'absences déjà approuvées sur l'équipe.
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
