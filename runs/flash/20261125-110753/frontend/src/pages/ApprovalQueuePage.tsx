import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import { leaveRequestsApi, teamsApi } from '../api/endpoints';
import type { LeaveRequest } from '../api/types';
import { Alert } from '../components/Alert';
import { EmptyState } from '../components/EmptyState';
import { ErrorState, describeError } from '../components/ErrorState';
import { OverlapBadge } from '../components/OverlapBadge';
import { Spinner } from '../components/Spinner';
import { formatDateTime, formatRange } from '../utils/dates';

type Mode = 'approve' | 'reject' | null;

export function ApprovalQueuePage() {
  const { user, isManager } = useAuth();
  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const [activeRequestId, setActiveRequestId] = useState<string | null>(null);
  const [mode, setMode] = useState<Mode>(null);
  const [rejectComment, setRejectComment] = useState('');
  const [acknowledged, setAcknowledged] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const teamId = user?.teamId ?? null;
  const reload = useCallback(() => setReloadKey((k) => k + 1), []);

  useEffect(() => {
    if (!isManager || !teamId) {
      setLoading(false);
      return;
    }
    let active = true;
    setLoading(true);
    setError(null);
    teamsApi
      .getPendingRequests(teamId)
      .then((data) => {
        if (!active) return;
        setRequests(data);
        setLoading(false);
      })
      .catch((err) => {
        if (!active) return;
        setError(err);
        setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [isManager, teamId, reloadKey]);

  function openAction(id: string, actionMode: 'approve' | 'reject') {
    setActiveRequestId(id);
    setMode(actionMode);
    setRejectComment('');
    setAcknowledged(false);
    setActionError(null);
    setSuccessMessage(null);
  }

  function cancelAction() {
    setActiveRequestId(null);
    setMode(null);
    setRejectComment('');
    setAcknowledged(false);
    setActionError(null);
  }

  async function submitDecision(request: LeaveRequest, action: 'APPROVE' | 'REJECT') {
    setSubmitting(true);
    setActionError(null);
    setSuccessMessage(null);

    const trimmedComment = rejectComment.trim();
    if (action === 'REJECT' && !trimmedComment) {
      setActionError('Un commentaire est obligatoire pour refuser une demande.');
      setSubmitting(false);
      return;
    }

    try {
      await leaveRequestsApi.decide(request.id, {
        action,
        comment: action === 'REJECT' ? trimmedComment : undefined,
        acknowledgeOverlapWarning: request.hasOverlapWarning ? acknowledged : false,
      });

      const employee = `${request.employeeFirstName ?? ''} ${request.employeeLastName ?? ''}`.trim();
      setRequests((prev) => prev.filter((r) => r.id !== request.id));
      setSuccessMessage(
        action === 'APPROVE'
          ? `La demande de ${employee} a été approuvée.`
          : `La demande de ${employee} a été refusée.`,
      );
      cancelAction();
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        // L'état a changé côté serveur : on recharge pour resynchroniser.
        reload();
      }
      setActionError(describeError(err));
    } finally {
      setSubmitting(false);
    }
  }

  function handleRejectSubmit(event: FormEvent<HTMLFormElement>, request: LeaveRequest) {
    event.preventDefault();
    void submitDecision(request, 'REJECT');
  }

  if (!isManager) {
    return (
      <>
        <h1>Validation des demandes</h1>
        <EmptyState title="Accès réservé aux managers">
          Cette page est accessible uniquement aux managers d'équipe.
        </EmptyState>
      </>
    );
  }

  if (!teamId) {
    return (
      <>
        <h1>Validation des demandes</h1>
        <EmptyState title="Aucune équipe managée">
          Votre profil n'est rattaché à aucune équipe. Contactez votre administrateur RH.
        </EmptyState>
      </>
    );
  }

  return (
    <div className="stack-lg">
      <div>
        <h1>Validation des demandes</h1>
        <p className="muted">Demandes en attente de votre décision pour votre équipe.</p>
      </div>

      {successMessage && (
        <Alert tone="success" title="Décision enregistrée">{successMessage}</Alert>
      )}

      {loading && <Spinner label="Chargement des demandes…" />}
      {error && <ErrorState error={error} onRetry={reload} />}

      {!loading && !error && requests.length === 0 && (
        <EmptyState title="Aucune demande en attente">
          Vous n'avez aucune demande à traiter pour le moment.
        </EmptyState>
      )}

      {!loading && !error && requests.length > 0 && (
        <div className="stack">
          {requests.map((req) => {
            const isActive = activeRequestId === req.id;
            const isMe = user?.id === req.employeeId;
            const employeeName = `${req.employeeFirstName ?? ''} ${req.employeeLastName ?? ''}`.trim();

            return (
              <article
                key={req.id}
                className="card request-card"
                aria-labelledby={`req-${req.id}-title`}
              >
                <header className="request-card-header">
                  <h2 id={`req-${req.id}-title`} style={{ margin: 0 }}>
                    {employeeName}
                  </h2>
                  <span className="badge badge-pending">En attente</span>
                </header>

                <div className="request-meta">
                  <div>
                    <strong>{req.leaveType.label}</strong> ({req.leaveType.code})
                  </div>
                  <div>
                    {formatRange(req.startDate, req.endDate)} —{' '}
                    <strong>{req.workingDays}</strong> jour{req.workingDays > 1 ? 's' : ''} ouvré
                    {req.workingDays > 1 ? 's' : ''}
                  </div>
                  <div className="text-sm muted">
                    Soumise le {formatDateTime(req.submittedAt)}
                  </div>
                  {req.employeeComment && (
                    <div className="text-sm">
                      <span className="section-label">Commentaire employé</span>
                      <p style={{ margin: 0 }}>{req.employeeComment}</p>
                    </div>
                  )}
                </div>

                <OverlapBadge
                  hasWarning={req.hasOverlapWarning}
                  ratio={req.overlapRatio}
                  details={req.overlapDetails}
                />

                {isMe && (
                  <Alert tone="warning" title="Auto-validation interdite">
                    Vous ne pouvez pas décider de votre propre demande.
                  </Alert>
                )}

                {!isActive && !isMe && (
                  <div className="request-actions">
                    <button
                      type="button"
                      className="btn btn-primary"
                      onClick={() => openAction(req.id, 'approve')}
                    >
                      Approuver
                    </button>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={() => openAction(req.id, 'reject')}
                    >
                      Refuser
                    </button>
                  </div>
                )}

                {isActive && mode === 'approve' && (
                  <div className="stack">
                    {req.hasOverlapWarning && (
                      <div className="checkbox-row">
                        <input
                          id={`ack-${req.id}`}
                          type="checkbox"
                          checked={acknowledged}
                          onChange={(e) => setAcknowledged(e.target.checked)}
                        />
                        <label htmlFor={`ack-${req.id}`}>
                          Je prends connaissance du chevauchement critique et je valide
                          malgré tout.
                        </label>
                      </div>
                    )}
                    {actionError && (
                      <Alert tone="error" title="Échec de l'approbation">{actionError}</Alert>
                    )}
                    <div className="request-actions">
                      <button
                        type="button"
                        className="btn btn-primary"
                        disabled={submitting || (req.hasOverlapWarning && !acknowledged)}
                        onClick={() => void submitDecision(req, 'APPROVE')}
                      >
                        {submitting ? 'Envoi…' : "Confirmer l'approbation"}
                      </button>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={cancelAction}
                        disabled={submitting}
                      >
                        Annuler
                      </button>
                    </div>
                  </div>
                )}

                {isActive && mode === 'reject' && (
                  <form className="reject-form" onSubmit={(e) => handleRejectSubmit(e, req)}>
                    <div className="field">
                      <label htmlFor={`reject-${req.id}`}>
                        Motif du refus <span aria-hidden="true">*</span>
                      </label>
                      <textarea
                        id={`reject-${req.id}`}
                        required
                        rows={3}
                        value={rejectComment}
                        onChange={(e) => setRejectComment(e.target.value)}
                        aria-describedby={`reject-${req.id}-hint`}
                        autoFocus
                      />
                      <span id={`reject-${req.id}-hint`} className="hint">
                        Ce commentaire sera transmis à l'employé.
                      </span>
                    </div>
                    {actionError && (
                      <Alert tone="error" title="Échec du refus">{actionError}</Alert>
                    )}
                    <div className="request-actions">
                      <button
                        type="submit"
                        className="btn btn-danger"
                        disabled={submitting || !rejectComment.trim()}
                      >
                        {submitting ? 'Envoi…' : 'Confirmer le refus'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={cancelAction}
                        disabled={submitting}
                      >
                        Annuler
                      </button>
                    </div>
                  </form>
                )}
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}
