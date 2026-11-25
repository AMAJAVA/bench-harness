import { useEffect, useMemo, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { api } from '../api/endpoints';
import { ApiError } from '../api/client';
import type { LeaveRequestDetail } from '../api/types';
import {
  describeError,
  fieldErrorsFrom,
  getBalanceShortages,
} from '../lib/errors';
import {
  formatDateTime,
  formatDays,
  formatMediumDate,
  formatRate,
  halfDayLabel,
  halfDayRangeLabel,
  initials,
  weekdayName,
} from '../lib/dates';
import { usePageTitle } from '../hooks/usePageTitle';
import { PageHeader } from '../components/PageHeader';
import { OverlapBadge, StatusBadge, TypeBadge } from '../components/Badges';
import {
  Alert,
  EmptyState,
  ErrorState,
  LoadingState,
  Spinner,
} from '../components/Feedback';
import {
  AlertTriangleIcon,
  CheckCircleIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ClockIcon,
  InboxIcon,
  InfoIcon,
  RefreshIcon,
  ShieldCheckIcon,
  UserIcon,
} from '../components/icons';

const PAGE_SIZE = 20;

const decisionSchema = z
  .object({
    decision: z.enum(['approve', 'reject']),
    comment: z
      .string()
      .max(1000, 'Le commentaire ne peut pas dépasser 1 000 caractères.'),
    acknowledged: z.boolean(),
  })
  .superRefine((values, context) => {
    if (values.decision === 'reject' && values.comment.trim().length === 0) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['comment'],
        message: 'Un commentaire non vide est obligatoire pour refuser.',
      });
    }
  });

type DecisionValues = z.infer<typeof decisionSchema>;

interface Notice {
  title: string;
  message: string;
}

function OverlapPanel({ overlap }: { overlap: LeaveRequestDetail['overlap'] }) {
  if (!overlap) {
    return null;
  }

  if (!overlap.applicable) {
    return (
      <Alert tone="info" title="Signal de chevauchement non applicable">
        L’équipe ne compte qu’un seul membre actif : aucun taux d’absence
        significatif ne peut être calculé.
      </Alert>
    );
  }

  if (!overlap.active) {
    return (
      <div className="overlap-ok" role="note">
        <span className="overlap-ok-icon" aria-hidden="true">
          <CheckCircleIcon size={22} />
        </span>
        <div>
          <strong>Aucun dépassement du seuil de 50 %</strong>
          <p>
            Taux maximal projeté inférieur ou égal à 50 % sur une équipe de{' '}
            {overlap.teamSize} membres. Évalué le{' '}
            {formatDateTime(overlap.evaluatedAt)}.
          </p>
        </div>
      </div>
    );
  }

  return (
    <Alert tone="warning" title="Signal de chevauchement actif" role="note">
      <p>
        Si cette demande est approuvée, le taux d’absence projeté atteindrait{' '}
        <strong>{overlap.maxRate === undefined ? '—' : formatRate(overlap.maxRate)}</strong>{' '}
        sur une équipe de {overlap.teamSize} membres, au-delà du seuil strict de
        50 %.
      </p>
      {overlap.breachingHalfDays.length > 0 ? (
        <>
          <p className="alert-subtitle">Demi-journées en dépassement</p>
          <ul className="alert-list">
            {overlap.breachingHalfDays.map((slot) => (
              <li key={`${slot.date}-${slot.halfDay}`}>
                {weekdayName(slot.date)} {formatMediumDate(slot.date)} —{' '}
                {halfDayLabel(slot.halfDay).toLowerCase()}
              </li>
            ))}
          </ul>
        </>
      ) : null}
      <p className="alert-detail">
        Signal recalculé le {formatDateTime(overlap.evaluatedAt)}. Il est
        consultatif : l’approbation reste possible après acquittement explicite.
      </p>
      {overlap.acknowledgement ? (
        <p className="alert-detail">
          Déjà acquitté par {overlap.acknowledgement.by.firstName}{' '}
          {overlap.acknowledgement.by.lastName} le{' '}
          {formatDateTime(overlap.acknowledgement.at)}.
        </p>
      ) : null}
    </Alert>
  );
}

export function ManagerQueuePage() {
  usePageTitle('File de validation');

  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  const detailRef = useRef<HTMLElement | null>(null);
  const queueHeadingRef = useRef<HTMLHeadingElement | null>(null);
  const skipAutoSelectRef = useRef(false);

  const queueQuery = useQuery({
    queryKey: ['manager-queue', page],
    queryFn: ({ signal }) => api.getManagerQueue(page, PAGE_SIZE, signal),
  });

  const detailQuery = useQuery({
    queryKey: ['manager-request', selectedId],
    queryFn: ({ signal }) =>
      api.getManagerRequest(selectedId as string, signal),
    enabled: Boolean(selectedId),
    staleTime: 0,
  });

  const items = queueQuery.data?.items ?? [];
  const totalItems = queueQuery.data?.totalItems ?? 0;
  const totalPages = queueQuery.data?.totalPages ?? 0;
  const detail = detailQuery.data;

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<DecisionValues>({
    resolver: zodResolver(decisionSchema),
    mode: 'onSubmit',
    reValidateMode: 'onChange',
    defaultValues: {
      decision: 'approve',
      comment: '',
      acknowledged: false,
    },
  });

  const decision = watch('decision');
  const acknowledged = watch('acknowledged');
  const requiresAcknowledgement = detail?.overlap?.active === true;

  const decisionMutation = useMutation({
    mutationFn: async (values: DecisionValues) => {
      if (!selectedId) {
        throw new Error('Aucune demande sélectionnée.');
      }

      if (values.decision === 'approve') {
        return api.approveLeaveRequest(selectedId, {
          comment: values.comment.trim() ? values.comment.trim() : undefined,
          overlapAcknowledged: values.acknowledged,
        });
      }

      return api.rejectLeaveRequest(selectedId, {
        comment: values.comment.trim(),
      });
    },
    onSuccess: (request) => {
      const name = `${request.employee.firstName} ${request.employee.lastName}`;
      const approved = request.status === 'APPROVED';
      setNotice({
        title: approved ? 'Demande approuvée' : 'Demande refusée',
        message: approved
          ? `La demande de ${name} a été approuvée et son solde a été mis à jour.`
          : `La demande de ${name} a été refusée avec le motif saisi.`,
      });
      skipAutoSelectRef.current = true;
      setSelectedId(null);
      reset({ decision: 'approve', comment: '', acknowledged: false });
      void queryClient.invalidateQueries({ queryKey: ['manager-queue'] });
      void queryClient.invalidateQueries({ queryKey: ['balances'] });
      void queryClient.invalidateQueries({ queryKey: ['calendar'] });
      window.requestAnimationFrame(() => queueHeadingRef.current?.focus());
    },
    onError: (error) => {
      const fields = fieldErrorsFrom(error);
      if (fields.comment) {
        setError('comment', { type: 'server', message: fields.comment });
      }

      if (error instanceof ApiError) {
        if (error.code === 'OVERLAP_ACK_REQUIRED') {
          setError('acknowledged', {
            type: 'server',
            message:
              'Le signal a été recalculé et reste actif. Cochez l’acquittement pour approuver.',
          });
          void detailQuery.refetch();
        }
        if (error.code === 'INVALID_STATE') {
          void queryClient.invalidateQueries({ queryKey: ['manager-queue'] });
        }
      }
    },
  });

  // Sélectionne automatiquement la première demande de la page courante et
  // conserve une sélection cohérente après une décision ou un changement de page.
  useEffect(() => {
    if (!queueQuery.isSuccess) {
      return;
    }

    if (skipAutoSelectRef.current) {
      skipAutoSelectRef.current = false;
      return;
    }

    const currentItems = queueQuery.data?.items ?? [];
    if (currentItems.length === 0) {
      if (selectedId) {
        setSelectedId(null);
      }
      return;
    }

    if (!selectedId || !currentItems.some((item) => item.id === selectedId)) {
      setSelectedId(currentItems[0]?.id ?? null);
    }
  }, [queueQuery.isSuccess, queueQuery.data, selectedId]);

  // Revenir à la dernière page non vide après une décision.
  useEffect(() => {
    if (
      queueQuery.data &&
      queueQuery.data.totalPages > 0 &&
      page > queueQuery.data.totalPages
    ) {
      setPage(queueQuery.data.totalPages);
    }
  }, [queueQuery.data, page]);

  // Réinitialise la décision quand la demande sélectionnée change.
  useEffect(() => {
    reset({ decision: 'approve', comment: '', acknowledged: false });
    decisionMutation.reset();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedId, reset]);

  const shortages = useMemo(
    () => getBalanceShortages(decisionMutation.error),
    [decisionMutation.error],
  );
  const decisionError = decisionMutation.error
    ? describeError(decisionMutation.error)
    : null;

  const selectRequest = (id: string) => {
    setSelectedId(id);
    setNotice(null);
    if (window.matchMedia('(max-width: 980px)').matches) {
      window.requestAnimationFrame(() => {
        detailRef.current?.scrollIntoView({ block: 'start' });
      });
    }
  };

  const onSubmit = async (values: DecisionValues) => {
    if (
      values.decision === 'approve' &&
      requiresAcknowledgement &&
      !values.acknowledged
    ) {
      setError('acknowledged', {
        type: 'manual',
        message:
          'Vous devez acquitter le signal de chevauchement avant d’approuver cette demande.',
      });
      return;
    }

    try {
      await decisionMutation.mutateAsync(values);
    } catch {
      // L'erreur est rendue via decisionMutation.error.
    }
  };

  const refreshAll = () => {
    void queueQuery.refetch();
    if (selectedId) {
      void detailQuery.refetch();
    }
  };

  const busy = queueQuery.isFetching || decisionMutation.isPending;

  return (
    <div className="page">
      <PageHeader
        eyebrow="Manager"
        title="File de validation"
        description="Demandes en attente de vos collaborateur·rices directs. Votre propre demande n’y figure jamais."
        actions={
          <button
            type="button"
            className="btn btn-secondary"
            onClick={refreshAll}
            disabled={busy}
          >
            <RefreshIcon size={18} />
            {queueQuery.isFetching ? 'Actualisation…' : 'Actualiser'}
          </button>
        }
      />

      {notice ? (
        <Alert
          tone="success"
          title={notice.title}
          role="status"
          actions={
            <button
              type="button"
              className="link-button"
              onClick={() => setNotice(null)}
            >
              Fermer
            </button>
          }
        >
          {notice.message}
        </Alert>
      ) : null}

      <div className="queue-grid">
        <section
          className="card queue-card"
          aria-labelledby="queue-title"
          aria-busy={queueQuery.isFetching}
        >
          <div className="card-header">
            <div>
              <h2
                id="queue-title"
                className="card-title"
                tabIndex={-1}
                ref={queueHeadingRef}
              >
                En attente
              </h2>
              <p className="card-subtitle">
                {queueQuery.isSuccess
                  ? `${totalItems} demande${totalItems > 1 ? 's' : ''} à traiter`
                  : 'Chargement de la file'}
              </p>
            </div>
            <span className="count-pill" aria-hidden="true">
              {totalItems}
            </span>
          </div>

          {queueQuery.isPending ? (
            <LoadingState label="Chargement des demandes…" rows={4} />
          ) : queueQuery.isError ? (
            <ErrorState
              title="File indisponible"
              message={describeError(queueQuery.error).message}
              code={describeError(queueQuery.error).code}
              onRetry={() => void queueQuery.refetch()}
            />
          ) : items.length === 0 ? (
            <EmptyState
              icon={<CheckCircleIcon size={24} />}
              title="Aucune demande en attente"
              description="Toutes les demandes de vos collaborateur·rices directs ont été traitées."
            />
          ) : (
            <>
              <ul className="queue-list">
                {items.map((item) => {
                  const selected = item.id === selectedId;
                  return (
                    <li key={item.id}>
                      <button
                        type="button"
                        className={`queue-item${selected ? ' is-selected' : ''}`}
                        onClick={() => selectRequest(item.id)}
                        aria-current={selected ? 'true' : undefined}
                      >
                        <span className="queue-item-top">
                          <span className="queue-item-title">
                            <span className="queue-name">
                              {item.leaveType.label} ·{' '}
                              {formatDays(item.totalDays)}
                            </span>
                            <span className="queue-sub">
                              Du {formatMediumDate(item.startDate)} au{' '}
                              {formatMediumDate(item.endDate)}
                            </span>
                          </span>
                          {item.overlapFlagged ? <OverlapBadge compact /> : null}
                        </span>
                        <span className="queue-item-foot">
                          <span className="queue-meta">
                            <ClockIcon size={14} />
                            Soumise le {formatDateTime(item.submittedAt)}
                          </span>
                          <span className="queue-chevron" aria-hidden="true">
                            <ChevronRightIcon size={18} />
                          </span>
                        </span>
                        {selected ? (
                          <span className="sr-only">Demande sélectionnée</span>
                        ) : null}
                      </button>
                    </li>
                  );
                })}
              </ul>

              {totalPages > 1 ? (
                <nav className="pagination" aria-label="Pagination de la file">
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={() => setPage((current) => Math.max(1, current - 1))}
                    disabled={page <= 1}
                  >
                    <ChevronLeftIcon size={18} />
                    Précédent
                  </button>
                  <span className="pagination-info">
                    Page {page} sur {totalPages}
                  </span>
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={() =>
                      setPage((current) => Math.min(totalPages, current + 1))
                    }
                    disabled={page >= totalPages}
                  >
                    Suivant
                    <ChevronRightIcon size={18} />
                  </button>
                </nav>
              ) : null}
            </>
          )}
        </section>

        <section
          className="card detail-card"
          aria-label="Détail de la demande"
          ref={detailRef}
          tabIndex={-1}
        >
          {!selectedId ? (
            <EmptyState
              icon={<InboxIcon size={24} />}
              title="Aucune demande sélectionnée"
              description="Sélectionnez une demande dans la file pour consulter son détail, le solde du demandeur et le signal de chevauchement."
            />
          ) : detailQuery.isPending ? (
            <LoadingState label="Chargement de la demande…" rows={5} />
          ) : detailQuery.isError ? (
            <ErrorState
              title="Demande indisponible"
              message={describeError(detailQuery.error).message}
              code={describeError(detailQuery.error).code}
              onRetry={() => void detailQuery.refetch()}
            />
          ) : detail ? (
            <>
              <div className="detail-head">
                <div className="detail-person">
                  <span className="avatar" aria-hidden="true">
                    {initials(detail.employee.firstName, detail.employee.lastName)}
                  </span>
                  <div>
                    <p className="eyebrow">Demande de congé</p>
                    <h2 id="detail-title" className="detail-title">
                      {detail.employee.firstName} {detail.employee.lastName}
                    </h2>
                    <p className="detail-period">
                      {halfDayRangeLabel(
                        detail.startDate,
                        detail.startHalfDay,
                        detail.endDate,
                        detail.endHalfDay,
                      )}
                    </p>
                  </div>
                </div>
                <StatusBadge status={detail.status} />
              </div>

              <dl className="detail-facts">
                <div>
                  <dt>Type</dt>
                  <dd>
                    <TypeBadge
                      label={detail.leaveType.label}
                      color={detail.leaveType.color}
                    />
                  </dd>
                </div>
                <div>
                  <dt>Jours décomptés</dt>
                  <dd className="detail-strong">{formatDays(detail.totalDays)}</dd>
                </div>
                <div>
                  <dt>Soumise le</dt>
                  <dd>{formatDateTime(detail.submittedAt)}</dd>
                </div>
              </dl>

              {detail.allocations.length > 1 ? (
                <div className="allocation-note">
                  <InfoIcon size={16} />
                  <span>
                    Demande répartie sur plusieurs années :{' '}
                    {detail.allocations
                      .map(
                        (allocation) =>
                          `${allocation.year} (${formatDays(allocation.days)})`,
                      )
                      .join(' · ')}
                    .
                  </span>
                </div>
              ) : null}

              {detail.comment ? (
                <div className="quote">
                  <h3 className="quote-title">Commentaire du demandeur</h3>
                  <p>{detail.comment}</p>
                </div>
              ) : null}

              {detail.employeeBalance ? (
                <section
                  className="balance-callout"
                  aria-labelledby="employee-balance-title"
                >
                  <div className="balance-callout-head">
                    <h3 id="employee-balance-title" className="balance-callout-title">
                      Solde {detail.employeeBalance.year}
                    </h3>
                    <span className="balance-callout-value">
                      {formatDays(detail.employeeBalance.availableDays)}
                    </span>
                  </div>
                  <p className="balance-callout-meta">
                    {formatDays(detail.employeeBalance.consumedDays)} consommés
                    sur {formatDays(detail.employeeBalance.grantedDays)}{' '}
                    octroyés.
                  </p>
                  <p className="balance-callout-note">
                    Solde disponible actuel, avant décision. Une demande en
                    attente ne réserve pas de jours.
                  </p>
                </section>
              ) : null}

              <OverlapPanel overlap={detail.overlap} />

              <form className="decision-form" onSubmit={handleSubmit(onSubmit)} noValidate>
                <fieldset className="decision-choice">
                  <legend className="decision-legend">Votre décision</legend>

                  <label
                    className={`choice${decision === 'approve' ? ' is-checked' : ''}`}
                  >
                    <input
                      type="radio"
                      value="approve"
                      {...register('decision')}
                    />
                    <span className="choice-body">
                      <span className="choice-title">Approuver</span>
                      <span className="choice-text">
                        Décompte le solde et notifie l’employé.
                      </span>
                    </span>
                  </label>

                  <label
                    className={`choice${decision === 'reject' ? ' is-checked' : ''}`}
                  >
                    <input
                      type="radio"
                      value="reject"
                      {...register('decision')}
                    />
                    <span className="choice-body">
                      <span className="choice-title">Refuser</span>
                      <span className="choice-text">
                        Le solde reste inchangé. Un motif est obligatoire.
                      </span>
                    </span>
                  </label>
                </fieldset>

                <div className="field">
                  <label htmlFor="decision-comment">
                    {decision === 'reject' ? (
                      <>
                        Motif du refus{' '}
                        <span className="field-required">(obligatoire)</span>
                      </>
                    ) : (
                      <>
                        Commentaire{' '}
                        <span className="field-optional">(facultatif)</span>
                      </>
                    )}
                  </label>
                  <textarea
                    id="decision-comment"
                    className="textarea"
                    rows={3}
                    maxLength={1000}
                    aria-invalid={Boolean(errors.comment)}
                    aria-describedby={
                      errors.comment
                        ? 'decision-comment-hint decision-comment-error'
                        : 'decision-comment-hint'
                    }
                    {...register('comment')}
                  />
                  <div className="field-meta">
                    <p className="field-hint" id="decision-comment-hint">
                      {decision === 'reject'
                        ? 'Le motif est visible de l’employé et de l’Admin RH.'
                        : 'Ce commentaire sera visible de l’employé et de l’Admin RH.'}
                    </p>
                    <span className="field-count">
                      {(watch('comment') ?? '').length}/1000
                    </span>
                  </div>
                  {errors.comment ? (
                    <p className="field-error" id="decision-comment-error">
                      {errors.comment.message}
                    </p>
                  ) : null}
                </div>

                {requiresAcknowledgement && decision === 'approve' ? (
                  <div className="acknowledgement">
                    <label className="checkbox">
                      <input
                        type="checkbox"
                        aria-invalid={Boolean(errors.acknowledged)}
                        aria-describedby={
                          errors.acknowledged ? 'acknowledgement-error' : undefined
                        }
                        {...register('acknowledged')}
                      />
                      <span>
                        J’ai pris connaissance du signal de chevauchement et
                        j’acquitte explicitement le dépassement du seuil de 50 %
                        pour cette approbation.
                      </span>
                    </label>
                    {errors.acknowledged ? (
                      <p className="field-error" id="acknowledgement-error">
                        {errors.acknowledged.message}
                      </p>
                    ) : null}
                  </div>
                ) : null}

                {decision === 'reject' ? (
                  <Alert tone="warning" title="Refus définitif">
                    Un refus est terminal : la demande ne pourra plus être
                    approuvée. L’employé pourra soumettre une nouvelle demande.
                  </Alert>
                ) : null}

                {decisionError ? (
                  <Alert tone="danger" title={decisionError.title} role="alert">
                    <p>{decisionError.message}</p>
                    {shortages.length > 0 ? (
                      <ul className="alert-list">
                        {shortages.map((shortage) => (
                          <li key={shortage.year}>
                            Année {shortage.year} :{' '}
                            {formatDays(shortage.availableDays)} disponibles pour{' '}
                            {formatDays(shortage.requestedDays)} demandés — il
                            manque {formatDays(shortage.missingDays)}.
                          </li>
                        ))}
                      </ul>
                    ) : null}
                  </Alert>
                ) : null}

                <div className="form-actions form-actions-inline">
                  <button
                    type="submit"
                    className={
                      decision === 'reject'
                        ? 'btn btn-danger btn-lg'
                        : 'btn btn-primary btn-lg'
                    }
                    disabled={decisionMutation.isPending || isSubmitting}
                  >
                    {decisionMutation.isPending || isSubmitting ? (
                      <Spinner />
                    ) : decision === 'reject' ? (
                      <AlertTriangleIcon size={18} />
                    ) : acknowledged && requiresAcknowledgement ? (
                      <ShieldCheckIcon size={18} />
                    ) : (
                      <CheckCircleIcon size={18} />
                    )}
                    {decisionMutation.isPending || isSubmitting
                      ? 'Enregistrement…'
                      : decision === 'reject'
                        ? 'Refuser la demande'
                        : 'Approuver la demande'}
                  </button>
                  <p className="form-actions-note">
                    La décision est définitive et notifie automatiquement
                    l’employé.
                  </p>
                </div>
              </form>
            </>
          ) : (
            <EmptyState
              icon={<UserIcon size={24} />}
              title="Demande indisponible"
              description="La demande n’est plus accessible. Rechargez la file pour actualiser son statut."
            />
          )}
        </section>
      </div>

      <p className="page-footnote">
        <ShieldCheckIcon size={16} />
        Le signal de chevauchement est recalculé à l’ouverture et juste avant la
        décision. Il ne bloque jamais définitivement une approbation.
      </p>
    </div>
  );
}
