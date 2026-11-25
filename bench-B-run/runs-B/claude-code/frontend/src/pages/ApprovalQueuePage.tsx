import { useEffect, useRef, useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { api, queryKeys } from '../api/endpoints';
import type { LeaveRequestSummary } from '../api/types';
import { Alert, EmptyState, ErrorState, LoadingState } from '../components/Feedback';
import { Pagination } from '../components/Pagination';
import { formatDateTimeParis, formatPeriod } from '../lib/dates';
import { formatDays } from '../lib/format';
import { usePageHeading } from '../lib/usePageHeading';
import { DecisionDialog } from './DecisionDialog';

const PAGE_SIZE = 20;

/** Écran 3 : file de validation du manager (US-08), décision dans une boîte de dialogue (US-09 à US-11). */
export function ApprovalQueuePage() {
  const headingRef = usePageHeading('Demandes à valider');
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<LeaveRequestSummary | null>(null);
  const [confirmation, setConfirmation] = useState<string | null>(null);
  const confirmationRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (confirmation) {
      confirmationRef.current?.focus();
    }
  }, [confirmation]);

  const queue = useQuery({
    queryKey: queryKeys.toDecide(page),
    queryFn: ({ signal }) => api.listToDecide(page, PAGE_SIZE, signal),
    placeholderData: keepPreviousData,
    refetchInterval: 60_000,
  });

  // Après une décision, la dernière page peut se vider : on revient à la précédente.
  useEffect(() => {
    if (queue.data && queue.data.items.length === 0 && page > 1) {
      setPage((current) => current - 1);
    }
  }, [queue.data, page]);

  const total = queue.data?.totalCount ?? 0;

  return (
    <div className="page">
      <header className="page__header">
        <h1 ref={headingRef} tabIndex={-1}>
          Demandes à valider
        </h1>
        <p className="muted">
          Demandes en attente de votre décision, les plus anciennes d'abord. L'indicateur « Chevauchement &gt; 50 % » est recalculé à
          chaque affichage.
        </p>
      </header>

      {/* Après une décision, la ligne (et son bouton) disparaît : le focus est placé sur la confirmation. */}
      <div ref={confirmationRef} tabIndex={-1} className="focus-target">
        {confirmation && <Alert tone="success">{confirmation}</Alert>}
      </div>

      <section className="card" aria-labelledby="queue-title" aria-busy={queue.isFetching}>
        <div className="section-head">
          <h2 id="queue-title">En attente</h2>
          {queue.data && (
            <p className="muted">
              {total} demande{total > 1 ? 's' : ''}
            </p>
          )}
        </div>

        {queue.isPending && <LoadingState label="Chargement des demandes à valider…" />}
        {queue.isError && !queue.data && (
          <ErrorState error={queue.error} title="Impossible de charger les demandes à valider." onRetry={() => void queue.refetch()} />
        )}
        {queue.isError && queue.data && (
          <Alert tone="warning">
            <p>La liste n'a pas pu être actualisée ; les informations affichées peuvent être obsolètes.</p>
          </Alert>
        )}
        {queue.data && queue.data.items.length === 0 && page === 1 && (
          <EmptyState title="Aucune demande en attente.">
            <p className="muted">Les nouvelles demandes de votre équipe apparaîtront ici.</p>
          </EmptyState>
        )}

        {queue.data && queue.data.items.length > 0 && (
          <>
            <ul className="request-list">
              {queue.data.items.map((item) => (
                <li key={item.id} className="request-card">
                  <div className="request-card__head">
                    <h3 className="request-card__name">{item.requester.fullName}</h3>
                    {item.overlapAlert?.isAlert && (
                      <p className="badge badge--alert">
                        <span aria-hidden="true">⚠</span> Chevauchement &gt; 50 %
                        <span className="badge__extra"> (jusqu'à {item.overlapAlert.maxRatePercent} %)</span>
                      </p>
                    )}
                  </div>
                  <dl className="details details--compact">
                    <div>
                      <dt>Type</dt>
                      <dd>{item.leaveType.label}</dd>
                    </div>
                    <div>
                      <dt>Période</dt>
                      <dd>{formatPeriod(item.startDate, item.endDate)}</dd>
                    </div>
                    <div>
                      <dt>Jours décomptés</dt>
                      <dd>{formatDays(item.daysCount)}</dd>
                    </div>
                    <div>
                      <dt>Soumise le</dt>
                      <dd>{formatDateTimeParis(item.submittedAtUtc)}</dd>
                    </div>
                  </dl>
                  <button
                    type="button"
                    className="button button--primary"
                    onClick={() => {
                      setConfirmation(null);
                      setSelected(item);
                    }}
                  >
                    Examiner et décider<span className="sr-only"> : demande de {item.requester.fullName}</span>
                  </button>
                </li>
              ))}
            </ul>
            <Pagination
              page={queue.data.page}
              pageSize={queue.data.pageSize}
              totalCount={queue.data.totalCount}
              onPageChange={setPage}
              label="Pages des demandes à valider"
            />
          </>
        )}
      </section>

      {selected && (
        <DecisionDialog
          key={selected.id}
          requestId={selected.id}
          requesterName={selected.requester.fullName}
          onClose={() => setSelected(null)}
          onDecided={(message) => {
            setSelected(null);
            setConfirmation(message);
          }}
        />
      )}
    </div>
  );
}
