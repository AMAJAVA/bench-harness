import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { api } from '../api/endpoints';
import { ApiError } from '../api/client';
import type { LeaveRequestDetail, SubmitLeaveRequestPayload } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import {
  currentYearInParis,
  estimateWorkingDays,
  formatDays,
  formatMediumDate,
  halfDayLabel,
  isValidIsoDate,
  isWeekend,
  todayInParis,
} from '../lib/dates';
import {
  describeError,
  fieldErrorsFrom,
  getBalanceShortages,
} from '../lib/errors';
import { usePageTitle } from '../hooks/usePageTitle';
import { PageHeader } from '../components/PageHeader';
import { StatusBadge, TypeBadge } from '../components/Badges';
import {
  Alert,
  EmptyState,
  ErrorState,
  LoadingState,
  Spinner,
} from '../components/Feedback';
import {
  AlertTriangleIcon,
  CalendarIcon,
  CheckCircleIcon,
  InfoIcon,
  PlusIcon,
  SendIcon,
} from '../components/icons';

const FIELD_NAMES = [
  'leaveTypeId',
  'startDate',
  'startHalfDay',
  'endDate',
  'endHalfDay',
  'comment',
] as const;

type FieldName = (typeof FIELD_NAMES)[number];

function isFieldName(value: string): value is FieldName {
  return (FIELD_NAMES as readonly string[]).includes(value);
}

const schema = z
  .object({
    leaveTypeId: z.string().min(1, 'Choisissez un type de congé.'),
    startDate: z
      .string()
      .min(1, 'Choisissez une date de début.')
      .refine(isValidIsoDate, 'La date de début est invalide.'),
    startHalfDay: z.enum(['AM', 'PM']),
    endDate: z
      .string()
      .min(1, 'Choisissez une date de fin.')
      .refine(isValidIsoDate, 'La date de fin est invalide.'),
    endHalfDay: z.enum(['AM', 'PM']),
    comment: z
      .string()
      .max(1000, 'Le commentaire ne peut pas dépasser 1 000 caractères.'),
  })
  .superRefine((values, context) => {
    const today = todayInParis();

    if (values.startDate && values.startDate < today) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['startDate'],
        message: 'La date de début doit être aujourd’hui ou une date future.',
      });
    }

    if (values.startDate && isWeekend(values.startDate)) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['startDate'],
        message: 'La date de début doit être un jour ouvré (lundi à vendredi).',
      });
    }

    if (values.endDate && isWeekend(values.endDate)) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['endDate'],
        message: 'La date de fin doit être un jour ouvré (lundi à vendredi).',
      });
    }

    if (
      values.startDate &&
      values.endDate &&
      values.endDate < values.startDate
    ) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['endDate'],
        message: 'La date de fin doit être postérieure ou égale à la date de début.',
      });
    }

    if (
      values.startDate &&
      values.endDate &&
      values.startDate === values.endDate &&
      values.startHalfDay === 'PM' &&
      values.endHalfDay === 'AM'
    ) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['endHalfDay'],
        message:
          'Sur une même journée, une demande ne peut pas commencer l’après-midi et finir le matin.',
      });
    }
  });

type FormValues = z.infer<typeof schema>;

function toPayload(values: FormValues): SubmitLeaveRequestPayload {
  const comment = values.comment.trim();
  return {
    leaveTypeId: values.leaveTypeId,
    startDate: values.startDate,
    startHalfDay: values.startHalfDay,
    endDate: values.endDate,
    endHalfDay: values.endHalfDay,
    comment: comment.length > 0 ? comment : undefined,
  };
}

export function SubmitLeavePage() {
  usePageTitle('Nouvelle demande');
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const currentYear = currentYearInParis();
  const [balanceYear, setBalanceYear] = useState(currentYear);
  const [success, setSuccess] = useState<LeaveRequestDetail | null>(null);
  const successHeadingRef = useRef<HTMLHeadingElement | null>(null);

  const typesQuery = useQuery({
    queryKey: ['leave-types'],
    queryFn: ({ signal }) => api.getLeaveTypes(signal),
  });

  const balancesQuery = useQuery({
    queryKey: ['balances', balanceYear],
    queryFn: ({ signal }) => api.getMyBalances(balanceYear, signal),
  });

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: 'onSubmit',
    reValidateMode: 'onChange',
    defaultValues: {
      leaveTypeId: '',
      startDate: '',
      startHalfDay: 'AM',
      endDate: '',
      endHalfDay: 'PM',
      comment: '',
    },
  });

  const mutation = useMutation({
    mutationFn: (payload: SubmitLeaveRequestPayload) =>
      api.submitLeaveRequest(payload),
    onSuccess: (request) => {
      setSuccess(request);
      reset({
        leaveTypeId: '',
        startDate: '',
        startHalfDay: 'AM',
        endDate: '',
        endHalfDay: 'PM',
        comment: '',
      });
      void queryClient.invalidateQueries({ queryKey: ['balances'] });
    },
    onError: (error) => {
      for (const [field, message] of Object.entries(fieldErrorsFrom(error))) {
        if (isFieldName(field)) {
          setError(field, { type: 'server', message });
        }
      }
    },
  });

  const [startDate, startHalfDay, endDate, endHalfDay] = watch([
    'startDate',
    'startHalfDay',
    'endDate',
    'endHalfDay',
  ]);

  const estimate = useMemo(
    () =>
      startDate && endDate
        ? estimateWorkingDays(startDate, startHalfDay, endDate, endHalfDay)
        : null,
    [startDate, startHalfDay, endDate, endHalfDay],
  );

  useEffect(() => {
    if (success) {
      successHeadingRef.current?.focus();
    }
  }, [success]);

  const hasManager = Boolean(user?.manager);
  const leaveTypes = typesQuery.data?.items ?? [];
  const typesUnavailable = typesQuery.isSuccess && leaveTypes.length === 0;
  const submitDisabled =
    mutation.isPending || isSubmitting || !hasManager || typesUnavailable;

  const onSubmit = async (values: FormValues) => {
    try {
      await mutation.mutateAsync(toPayload(values));
    } catch {
      // L'erreur est affichée via mutation.error et les erreurs de champ.
    }
  };

  const startNewRequest = () => {
    setSuccess(null);
    mutation.reset();
  };

  const serverError = mutation.error;
  const errorInfo = serverError ? describeError(serverError) : null;
  const shortages = getBalanceShortages(serverError);
  const conflict =
    serverError instanceof ApiError ? serverError.problem.conflict : undefined;
  const today = todayInParis();

  return (
    <div className="page">
      <PageHeader
        eyebrow="Congés"
        title="Nouvelle demande"
        description="Choisissez un type, des dates et des demi-journées. Votre manager recevra une notification dès l’enregistrement."
        actions={
          <Link to="/calendrier" className="btn btn-secondary">
            <CalendarIcon size={18} />
            Voir le calendrier
          </Link>
        }
      />

      {success ? (
        <section className="card success-card" aria-labelledby="success-title">
          <span className="success-icon" aria-hidden="true">
            <CheckCircleIcon size={30} />
          </span>
          <p className="eyebrow">Demande enregistrée</p>
          <h2
            id="success-title"
            className="success-title"
            tabIndex={-1}
            ref={successHeadingRef}
          >
            Votre demande est en attente de validation
          </h2>
          <p className="success-lead">
            Elle a bien été transmise à votre manager. Le solde ne sera débité
            qu’au moment de l’approbation.
          </p>

          <dl className="summary-grid">
            <div>
              <dt>Type</dt>
              <dd>
                <TypeBadge
                  label={success.leaveType.label}
                  color={success.leaveType.color}
                />
              </dd>
            </div>
            <div>
              <dt>Période</dt>
              <dd>
                Du {formatMediumDate(success.startDate)} (
                {halfDayLabel(success.startHalfDay).toLowerCase()}) au{' '}
                {formatMediumDate(success.endDate)} (
                {halfDayLabel(success.endHalfDay).toLowerCase()})
              </dd>
            </div>
            <div>
              <dt>Jours décomptés</dt>
              <dd>{formatDays(success.totalDays)}</dd>
            </div>
            <div>
              <dt>Statut</dt>
              <dd>
                <StatusBadge status={success.status} />
              </dd>
            </div>
          </dl>

          <Alert tone="info" title="Et maintenant ?">
            Votre manager décide depuis sa file de validation. En cas de refus,
            un commentaire est obligatoire. Vous pouvez suivre l’état de vos
            demandes auprès de votre manager.
          </Alert>

          <div className="success-actions">
            <button
              type="button"
              className="btn btn-primary"
              onClick={startNewRequest}
            >
              <PlusIcon size={18} />
              Soumettre une autre demande
            </button>
            <Link to="/calendrier" className="btn btn-secondary">
              <CalendarIcon size={18} />
              Consulter le calendrier
            </Link>
          </div>
        </section>
      ) : (
        <div className="page-grid">
          <section className="card" aria-labelledby="form-title">
            <div className="card-header">
              <div>
                <h2 id="form-title" className="card-title">
                  Détail de la demande
                </h2>
                <p className="card-subtitle">
                  Les dates de début et de fin doivent être des jours ouvrés.
                </p>
              </div>
            </div>

            {!hasManager ? (
              <Alert tone="warning" title="Aucun manager direct actif">
                Votre compte n’est rattaché à aucun manager direct actif. La
                soumission est bloquée tant que l’Admin RH n’a pas désigné un
                manager.
              </Alert>
            ) : null}

            <form className="form" onSubmit={handleSubmit(onSubmit)} noValidate>
              <div className="field">
                <label htmlFor="leave-type">Type de congé</label>
                <select
                  id="leave-type"
                  className="select"
                  aria-invalid={Boolean(errors.leaveTypeId)}
                  aria-describedby={
                    errors.leaveTypeId
                      ? 'leave-type-error leave-type-hint'
                      : 'leave-type-hint'
                  }
                  disabled={typesQuery.isPending}
                  {...register('leaveTypeId')}
                >
                  <option value="">
                    {typesQuery.isPending
                      ? 'Chargement des types…'
                      : 'Sélectionnez un type'}
                  </option>
                  {leaveTypes.map((type) => (
                    <option key={type.id} value={type.id}>
                      {type.label}
                    </option>
                  ))}
                </select>
                <p className="field-hint" id="leave-type-hint">
                  Seuls les types actifs sont proposés. Tous consomment un solde.
                </p>
                {errors.leaveTypeId ? (
                  <p className="field-error" id="leave-type-error">
                    {errors.leaveTypeId.message}
                  </p>
                ) : null}
                {typesQuery.isError ? (
                  <p className="field-error" role="alert">
                    {describeError(typesQuery.error).message}{' '}
                    <button
                      type="button"
                      className="link-button"
                      onClick={() => void typesQuery.refetch()}
                    >
                      Réessayer
                    </button>
                  </p>
                ) : null}
              </div>

              <div className="field-row">
                <div className="field field-grow">
                  <label htmlFor="start-date">Date de début</label>
                  <input
                    id="start-date"
                    type="date"
                    className="input"
                    min={today}
                    aria-invalid={Boolean(errors.startDate)}
                    aria-describedby={
                      errors.startDate ? 'start-date-error' : undefined
                    }
                    {...register('startDate')}
                  />
                  {errors.startDate ? (
                    <p className="field-error" id="start-date-error">
                      {errors.startDate.message}
                    </p>
                  ) : null}
                </div>

                <div className="field field-half">
                  <label htmlFor="start-half">Début</label>
                  <select
                    id="start-half"
                    className="select"
                    {...register('startHalfDay')}
                  >
                    <option value="AM">Matin</option>
                    <option value="PM">Après-midi</option>
                  </select>
                </div>
              </div>

              <div className="field-row">
                <div className="field field-grow">
                  <label htmlFor="end-date">Date de fin</label>
                  <input
                    id="end-date"
                    type="date"
                    className="input"
                    min={startDate || today}
                    aria-invalid={Boolean(errors.endDate)}
                    aria-describedby={
                      errors.endDate ? 'end-date-error' : undefined
                    }
                    {...register('endDate')}
                  />
                  {errors.endDate ? (
                    <p className="field-error" id="end-date-error">
                      {errors.endDate.message}
                    </p>
                  ) : null}
                </div>

                <div className="field field-half">
                  <label htmlFor="end-half">Fin</label>
                  <select
                    id="end-half"
                    className="select"
                    aria-invalid={Boolean(errors.endHalfDay)}
                    aria-describedby={
                      errors.endHalfDay ? 'end-half-error' : undefined
                    }
                    {...register('endHalfDay')}
                  >
                    <option value="AM">Matin</option>
                    <option value="PM">Après-midi</option>
                  </select>
                  {errors.endHalfDay ? (
                    <p className="field-error" id="end-half-error">
                      {errors.endHalfDay.message}
                    </p>
                  ) : null}
                </div>
              </div>

              <div className="estimate" aria-live="polite">
                <span className="estimate-icon" aria-hidden="true">
                  <InfoIcon size={18} />
                </span>
                <div className="estimate-text">
                  <span className="estimate-label">Décompte indicatif</span>
                  <span className="estimate-value">
                    {estimate === null ? '—' : formatDays(estimate)}
                  </span>
                  <p className="estimate-note">
                    Estimation en jours ouvrés. Le décompte officiel est calculé
                    par le serveur à la soumission.
                  </p>
                </div>
              </div>

              <div className="field">
                <label htmlFor="comment">
                  Commentaire <span className="field-optional">(facultatif)</span>
                </label>
                <textarea
                  id="comment"
                  className="textarea"
                  rows={3}
                  maxLength={1000}
                  aria-invalid={Boolean(errors.comment)}
                  aria-describedby={
                    errors.comment
                      ? 'comment-hint comment-count comment-error'
                      : 'comment-hint comment-count'
                  }
                  {...register('comment')}
                />
                <div className="field-meta">
                  <p className="field-hint" id="comment-hint">
                    Visible de votre manager et de l’Admin RH. 1 000 caractères
                    maximum.
                  </p>
                  <span className="field-count" id="comment-count">
                    {(watch('comment') ?? '').length}/1000
                  </span>
                </div>
                {errors.comment ? (
                  <p className="field-error" id="comment-error">
                    {errors.comment.message}
                  </p>
                ) : null}
              </div>

              {errorInfo ? (
                <Alert tone="danger" title={errorInfo.title} role="alert">
                  <p>{errorInfo.message}</p>
                  {conflict ? (
                    <p className="alert-detail">
                      Conflit le {formatMediumDate(conflict.date)} (
                      {halfDayLabel(conflict.halfDay).toLowerCase()}).
                    </p>
                  ) : null}
                  {shortages.length > 0 ? (
                    <ul className="alert-list">
                      {shortages.map((shortage) => (
                        <li key={`${shortage.year}-${shortage.code ?? ''}`}>
                          {shortage.code ?? 'Solde'} {shortage.year} :{' '}
                          {formatDays(shortage.availableDays)} disponibles pour{' '}
                          {formatDays(shortage.requestedDays)} demandés — il
                          manque {formatDays(shortage.missingDays)}.
                        </li>
                      ))}
                    </ul>
                  ) : null}
                </Alert>
              ) : null}

              {typesUnavailable && !typesQuery.isPending ? (
                <EmptyState
                  compact
                  title="Aucun type de congé actif"
                  description="L’Admin RH doit activer au moins un type de congé pour permettre les demandes."
                />
              ) : null}

              <div className="form-actions">
                <button
                  type="submit"
                  className="btn btn-primary btn-lg"
                  disabled={submitDisabled}
                >
                  {mutation.isPending || isSubmitting ? (
                    <Spinner />
                  ) : (
                    <SendIcon size={18} />
                  )}
                  {mutation.isPending || isSubmitting
                    ? 'Envoi en cours…'
                    : 'Soumettre la demande'}
                </button>
                <p className="form-actions-note">
                  Aucun brouillon : une demande soumise ne peut plus être
                  modifiée. En cas d’erreur, annulez-la puis recommencez.
                </p>
              </div>
            </form>
          </section>

          <aside className="page-aside" aria-label="Mes soldes">
            <section className="card" aria-labelledby="balances-title">
              <div className="card-header">
                <div>
                  <h2 id="balances-title" className="card-title">
                    Mes soldes
                  </h2>
                  <p className="card-subtitle">
                    Disponible = octroyé − consommé
                  </p>
                </div>
                <label className="year-select">
                  <span className="sr-only">Année des soldes</span>
                  <select
                    className="select select-sm"
                    value={balanceYear}
                    onChange={(event) =>
                      setBalanceYear(Number(event.target.value))
                    }
                  >
                    {[currentYear - 1, currentYear, currentYear + 1].map(
                      (year) => (
                        <option key={year} value={year}>
                          {year}
                        </option>
                      ),
                    )}
                  </select>
                </label>
              </div>

              {balancesQuery.isPending ? (
                <LoadingState label="Chargement de vos soldes…" rows={3} />
              ) : balancesQuery.isError ? (
                <ErrorState
                  title="Soldes indisponibles"
                  message={describeError(balancesQuery.error).message}
                  onRetry={() => void balancesQuery.refetch()}
                />
              ) : balancesQuery.data.balances.length === 0 ? (
                <EmptyState
                  compact
                  title="Aucun solde attribué"
                  description={`Aucun solde n’est attribué pour ${balanceYear}. Un solde non attribué vaut 0 jour.`}
                  icon={<AlertTriangleIcon size={22} />}
                />
              ) : (
                <ul className="balance-list">
                  {balancesQuery.data.balances.map((balance) => {
                    const ratio =
                      balance.grantedDays > 0
                        ? Math.min(
                            100,
                            Math.max(
                              0,
                              (balance.consumedDays / balance.grantedDays) * 100,
                            ),
                          )
                        : 0;
                    return (
                      <li key={balance.leaveTypeId} className="balance-item">
                        <div className="balance-top">
                          <TypeBadge
                            label={balance.label}
                            color={balance.color}
                          />
                          <span className="balance-available">
                            {formatDays(balance.availableDays)}
                          </span>
                        </div>
                        <div className="balance-meter" aria-hidden="true">
                          <span
                            className="balance-meter-fill"
                            style={{ width: `${ratio}%` }}
                          />
                        </div>
                        <p className="balance-meta">
                          {formatDays(balance.consumedDays)} consommés sur{' '}
                          {formatDays(balance.grantedDays)}
                        </p>
                      </li>
                    );
                  })}
                </ul>
              )}

              <p className="card-note">
                Les demandes en attente ne réservent pas de solde : le contrôle
                définitif a lieu à l’approbation.
              </p>
            </section>

            <section className="card card-soft" aria-labelledby="tip-title">
              <h2 id="tip-title" className="card-title">
                Bon à savoir
              </h2>
              <ul className="tip-list">
                <li>Une demi-journée vaut 0,5 jour.</li>
                <li>Les week-ends ne sont pas décomptés.</li>
                <li>Une demande peut chevaucher deux années civiles.</li>
              </ul>
            </section>
          </aside>
        </div>
      )}
    </div>
  );
}
