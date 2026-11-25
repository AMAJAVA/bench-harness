import { useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '../api/client';
import { api, queryKeys } from '../api/endpoints';
import { errorMessage, fieldErrors } from '../api/errors';
import type { Guid, LeaveRequest, OverlapAnalysis } from '../api/types';
import { Alert, ErrorState, LoadingState, Spinner } from '../components/Feedback';
import { OverlapDetails } from '../components/OverlapDetails';
import { formatDateTimeParis, formatPeriod } from '../lib/dates';
import { formatDays, formatRate, STATUS_LABELS } from '../lib/format';

const COMMENT_MAX = 1000;

type Action = 'approve' | 'reject';

interface DecisionDialogProps {
  requestId: Guid;
  requesterName: string;
  onClose: () => void;
  onDecided: (message: string) => void;
}

/**
 * Examen et décision d'une demande (US-09, US-10, US-11) dans une boîte de dialogue modale native :
 * piège du focus, fermeture par Échap et restitution du focus assurés par le navigateur.
 */
export function DecisionDialog({ requestId, requesterName, onClose, onDecided }: DecisionDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  // Pas de close() au démontage : retirer l'élément du DOM le sort de la couche supérieure, et un close()
  // déclencherait un évènement `close` asynchrone (double montage du StrictMode) qui refermerait la fenêtre.
  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) {
      dialog.showModal();
    }
  }, []);

  return (
    <dialog ref={dialogRef} className="dialog" aria-labelledby={titleId} onClose={onClose}>
      <div className="dialog__header">
        <h2 id={titleId} className="dialog__title">
          Demande de {requesterName}
        </h2>
        <button type="button" className="button button--ghost dialog__close" onClick={() => dialogRef.current?.close()}>
          <span aria-hidden="true">✕</span>
          <span className="sr-only">Fermer</span>
        </button>
      </div>
      <DecisionContent requestId={requestId} onDecided={onDecided} />
    </dialog>
  );
}

function DecisionContent({ requestId, onDecided }: { requestId: Guid; onDecided: (message: string) => void }) {
  const detail = useQuery({
    queryKey: queryKeys.leaveRequest(requestId),
    queryFn: ({ signal }) => api.getLeaveRequest(requestId, signal),
    staleTime: 0,
  });

  if (detail.isPending) {
    return <LoadingState label="Chargement de la demande…" />;
  }
  if (detail.isError) {
    return (
      <ErrorState
        error={detail.error}
        title="Impossible d'afficher cette demande."
        onRetry={detail.error instanceof ApiError && detail.error.status === 404 ? undefined : () => void detail.refetch()}
      />
    );
  }
  return <DecisionForm request={detail.data} onDecided={onDecided} />;
}

function DecisionForm({ request, onDecided }: { request: LeaveRequest; onDecided: (message: string) => void }) {
  const queryClient = useQueryClient();
  const [comment, setComment] = useState('');
  const [acknowledged, setAcknowledged] = useState(false);
  const [clientError, setClientError] = useState<{ field: 'comment' | 'acknowledged'; message: string } | null>(null);
  // Analyse renvoyée par un 422 OVERLAP_NOT_ACKNOWLEDGED : plus récente que celle du détail (RB-17).
  const [freshOverlap, setFreshOverlap] = useState<OverlapAnalysis | null>(null);
  const ackRef = useRef<HTMLInputElement>(null);
  const commentRef = useRef<HTMLTextAreaElement>(null);
  const errorRef = useRef<HTMLDivElement>(null);

  const overlap = freshOverlap ?? request.overlap;
  const requiresAck = overlap?.isAlert === true;

  const decide = useMutation({
    mutationFn: ({ action, text }: { action: Action; text: string }) =>
      action === 'approve'
        ? api.approve(request.id, { comment: text === '' ? null : text, overlapAcknowledged: requiresAck && acknowledged })
        : api.reject(request.id, { comment: text }),
    onSuccess: (decided, { action }) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.toDecideAll });
      queryClient.setQueryData(queryKeys.leaveRequest(decided.id), decided);
      onDecided(
        `Demande de ${decided.requester.fullName} (${decided.leaveType.label}, ${formatPeriod(decided.startDate, decided.endDate)}) ${
          action === 'approve' ? 'approuvée' : 'refusée'
        }. L'employé est prévenu par email.`,
      );
    },
    onError: (error) => {
      if (error instanceof ApiError) {
        if (error.code === 'OVERLAP_NOT_ACKNOWLEDGED' && error.problem?.overlap) {
          setFreshOverlap(error.problem.overlap);
          setAcknowledged(false);
          window.setTimeout(() => ackRef.current?.focus(), 0);
          return;
        }
        if (error.status === 409 || error.status === 404 || error.status === 403) {
          // La demande a changé (décidée ailleurs, rattachement modifié…) : la file doit être rafraîchie.
          void queryClient.invalidateQueries({ queryKey: queryKeys.toDecideAll });
        }
      }
      window.setTimeout(() => errorRef.current?.focus(), 0);
    },
  });

  const serverFields = fieldErrors(decide.error);
  const commentError =
    clientError?.field === 'comment'
      ? clientError.message
      : decide.error instanceof ApiError && decide.error.code === 'REJECTION_COMMENT_REQUIRED'
        ? errorMessage(decide.error)
        : serverFields.comment;
  const ackError = clientError?.field === 'acknowledged' ? clientError.message : undefined;
  const isFinal = decide.error instanceof ApiError && [403, 404, 409].includes(decide.error.status) && decide.error.code !== 'CONCURRENCY_CONFLICT';

  function submit(action: Action) {
    setClientError(null);
    const text = comment.trim();
    if (action === 'reject' && text === '') {
      setClientError({ field: 'comment', message: 'Un commentaire est obligatoire pour refuser une demande.' });
      commentRef.current?.focus();
      return;
    }
    if (action === 'approve' && requiresAck && !acknowledged) {
      setClientError({ field: 'acknowledged', message: "Cochez la case pour confirmer l'approbation malgré le chevauchement." });
      ackRef.current?.focus();
      return;
    }
    decide.mutate({ action, text });
  }

  // Pas de soumission implicite (touche Entrée) : chaque décision passe par un bouton explicite.
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
  }

  const pendingAction = decide.isPending ? decide.variables?.action : undefined;
  const showOverlapError = decide.error instanceof ApiError && decide.error.code === 'OVERLAP_NOT_ACKNOWLEDGED';

  return (
    <div className="dialog__body">
      <dl className="details">
        <div>
          <dt>Type</dt>
          <dd>{request.leaveType.label}</dd>
        </div>
        <div>
          <dt>Période</dt>
          <dd>{formatPeriod(request.startDate, request.endDate)}</dd>
        </div>
        <div>
          <dt>Jours décomptés</dt>
          <dd>{formatDays(request.daysCount)}</dd>
        </div>
        <div>
          <dt>Soumise le</dt>
          <dd>{formatDateTimeParis(request.submittedAtUtc)}</dd>
        </div>
        <div>
          <dt>Statut</dt>
          <dd>{STATUS_LABELS[request.status]}</dd>
        </div>
        <div className="details__wide">
          <dt>Commentaire de l'employé</dt>
          <dd>{request.comment ?? <span className="muted">Aucun</span>}</dd>
        </div>
      </dl>

      {overlap && <OverlapDetails overlap={overlap} headingId={`overlap-${request.id}`} />}

      {decide.isError && (
        <div ref={errorRef} tabIndex={-1} className="focus-target">
          <Alert tone="error" title={showOverlapError ? 'Approbation non enregistrée' : 'Décision non enregistrée'}>
            <p>{errorMessage(decide.error)}</p>
            {isFinal && <p>Fermez cette fenêtre : la liste des demandes a été actualisée.</p>}
          </Alert>
        </div>
      )}

      {request.status !== 'Pending' ? (
        <Alert tone="info">
          <p>Cette demande n'est plus en attente : aucune décision n'est possible.</p>
        </Alert>
      ) : (
        <form className="form" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor={`decision-comment-${request.id}`}>Commentaire pour l'employé</label>
            <textarea
              ref={commentRef}
              id={`decision-comment-${request.id}`}
              rows={3}
              maxLength={COMMENT_MAX}
              value={comment}
              onChange={(event) => {
                setComment(event.target.value);
                if (clientError?.field === 'comment') setClientError(null);
              }}
              aria-invalid={commentError ? true : undefined}
              aria-describedby={[`decision-comment-hint-${request.id}`, commentError ? `decision-comment-error-${request.id}` : '']
                .filter(Boolean)
                .join(' ')}
            />
            <p id={`decision-comment-hint-${request.id}`} className="field__hint">
              Facultatif pour une approbation, <strong>obligatoire pour un refus</strong>. {comment.length} / {COMMENT_MAX} caractères.
            </p>
            {commentError && (
              <p id={`decision-comment-error-${request.id}`} className="field__error">
                {commentError}
              </p>
            )}
          </div>

          {requiresAck && (
            <div className="field field--checkbox">
              <input
                ref={ackRef}
                id={`decision-ack-${request.id}`}
                type="checkbox"
                checked={acknowledged}
                onChange={(event) => {
                  setAcknowledged(event.target.checked);
                  if (clientError?.field === 'acknowledged') setClientError(null);
                }}
                aria-invalid={ackError ? true : undefined}
                aria-describedby={ackError ? `decision-ack-error-${request.id}` : undefined}
              />
              <label htmlFor={`decision-ack-${request.id}`}>
                J'ai pris connaissance du chevauchement{overlap?.maxRate ? ` (${formatRate(overlap.maxRate)})` : ''} et j'approuve en
                connaissance de cause.
              </label>
              {ackError && (
                <p id={`decision-ack-error-${request.id}`} className="field__error">
                  {ackError}
                </p>
              )}
            </div>
          )}

          <div className="form__actions form__actions--split">
            <button type="button" className="button button--danger" disabled={decide.isPending} onClick={() => submit('reject')}>
              {pendingAction === 'reject' && <Spinner />}
              Refuser
            </button>
            <button type="button" className="button button--primary" disabled={decide.isPending} onClick={() => submit('approve')}>
              {pendingAction === 'approve' && <Spinner />}
              Approuver
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
