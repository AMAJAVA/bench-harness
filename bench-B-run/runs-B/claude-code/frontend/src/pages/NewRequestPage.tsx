import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient, type UseQueryResult } from '@tanstack/react-query';
import { ApiError } from '../api/client';
import { api, queryKeys } from '../api/endpoints';
import { errorMessage, fieldErrors } from '../api/errors';
import type { Balance, LeaveRequest, LeaveType, SimulateRequest, Simulation } from '../api/types';
import { Alert, EmptyState, ErrorState, LoadingState, Spinner } from '../components/Feedback';
import { formatDate, formatPeriod, isIsoDate } from '../lib/dates';
import { blockingReasonLabel, formatDays, STATUS_LABELS } from '../lib/format';
import { useDebouncedValue } from '../lib/useDebouncedValue';
import { usePageHeading } from '../lib/usePageHeading';

const COMMENT_MAX = 500;

interface FormState {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  comment: string;
}

const EMPTY_FORM: FormState = { leaveTypeId: '', startDate: '', endDate: '', comment: '' };

type FieldName = keyof FormState;

function validate(form: FormState): Partial<Record<FieldName, string>> {
  const errors: Partial<Record<FieldName, string>> = {};
  if (!form.leaveTypeId) {
    errors.leaveTypeId = 'Choisissez un type de congé.';
  }
  if (!isIsoDate(form.startDate)) {
    errors.startDate = 'Saisissez une date de début valide.';
  }
  if (!isIsoDate(form.endDate)) {
    errors.endDate = 'Saisissez une date de fin valide.';
  } else if (isIsoDate(form.startDate) && form.endDate < form.startDate) {
    errors.endDate = 'La date de fin doit être postérieure ou égale à la date de début.';
  }
  if (form.comment.length > COMMENT_MAX) {
    errors.comment = `Le commentaire est limité à ${COMMENT_MAX} caractères.`;
  }
  return errors;
}

function simulationKey(form: FormState): string {
  const ready =
    form.leaveTypeId && isIsoDate(form.startDate) && isIsoDate(form.endDate) && form.endDate >= form.startDate;
  return ready ? [form.leaveTypeId, form.startDate, form.endDate].join('|') : '';
}

function parseSimulationKey(key: string): SimulateRequest | null {
  const [leaveTypeId, startDate, endDate] = key.split('|');
  return leaveTypeId && startDate && endDate ? { leaveTypeId, startDate, endDate } : null;
}

/** Écran 1 : soumettre une demande de congé (US-03 simulation, US-04 soumission, US-02 soldes). */
export function NewRequestPage() {
  const headingRef = usePageHeading('Nouvelle demande de congé');
  const queryClient = useQueryClient();

  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [clientErrors, setClientErrors] = useState<Partial<Record<FieldName, string>>>({});
  const [created, setCreated] = useState<LeaveRequest | null>(null);
  const successRef = useRef<HTMLDivElement>(null);
  const formErrorRef = useRef<HTMLDivElement>(null);

  const leaveTypes = useQuery({ queryKey: queryKeys.leaveTypes, queryFn: ({ signal }) => api.getLeaveTypes(signal), staleTime: 5 * 60_000 });
  const balances = useQuery({ queryKey: queryKeys.balances, queryFn: ({ signal }) => api.getMyBalances(signal) });

  // Simulation automatique (US-03), après une courte pause de saisie.
  const debouncedKey = useDebouncedValue(simulationKey(form), 400);
  const simulationInput = parseSimulationKey(debouncedKey);
  const simulation = useQuery({
    queryKey: simulationInput ? queryKeys.simulation(simulationInput) : ['simulation', 'idle'],
    queryFn: ({ signal }) => api.simulate(simulationInput as SimulateRequest, signal),
    enabled: simulationInput !== null,
    retry: false,
    staleTime: 30_000,
  });

  const submit = useMutation({
    mutationFn: api.submitLeaveRequest,
    onSuccess: (request) => {
      setCreated(request);
      setForm(EMPTY_FORM);
      setClientErrors({});
      void queryClient.invalidateQueries({ queryKey: queryKeys.balances });
      void queryClient.removeQueries({ queryKey: ['simulation'] });
    },
  });

  useEffect(() => {
    if (created) {
      successRef.current?.focus();
    }
  }, [created]);

  useEffect(() => {
    if (submit.isError) {
      formErrorRef.current?.focus();
    }
  }, [submit.isError, submit.error]);

  const serverFieldErrors = fieldErrors(submit.error);
  const errors: Partial<Record<FieldName, string>> = { ...serverFieldErrors, ...clientErrors };

  function update(name: FieldName, value: string) {
    setForm((current) => ({ ...current, [name]: value }));
    setClientErrors((current) => ({ ...current, [name]: undefined }));
    setCreated(null);
    if (submit.isError) {
      submit.reset();
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const found = validate(form);
    setClientErrors(found);
    const firstInvalid = (Object.keys(found) as FieldName[])[0];
    if (firstInvalid) {
      document.getElementById(`request-${firstInvalid}`)?.focus();
      return;
    }
    const comment = form.comment.trim();
    submit.mutate({
      leaveTypeId: form.leaveTypeId,
      startDate: form.startDate,
      endDate: form.endDate,
      comment: comment === '' ? null : comment,
    });
  }

  const selectedType = leaveTypes.data?.find((type) => type.id === form.leaveTypeId);
  const describedBy = (name: FieldName, ...extra: string[]) =>
    [...extra, errors[name] ? `request-${name}-error` : ''].filter(Boolean).join(' ') || undefined;

  return (
    <div className="page">
      <header className="page__header">
        <h1 ref={headingRef} tabIndex={-1}>
          Nouvelle demande de congé
        </h1>
        <p className="muted">Le nombre de jours décomptés et votre solde restant sont calculés avant l'envoi.</p>
      </header>

      {created && (
        <div ref={successRef} tabIndex={-1} className="focus-target">
          <Alert tone="success" title="Demande envoyée">
            <p>
              {created.leaveType.label} {formatPeriod(created.startDate, created.endDate)} : {formatDays(created.daysCount)} décompté
              {created.daysCount > 1 ? 's' : ''}. Statut : <strong>{STATUS_LABELS[created.status]}</strong>.
            </p>
            <p>Votre manager est prévenu par email. Le solde n'est débité qu'à l'approbation.</p>
          </Alert>
        </div>
      )}

      <div className="layout-split">
        <section className="card" aria-labelledby="request-form-title">
          <h2 id="request-form-title">Votre demande</h2>

          {submit.isError && (
            <div ref={formErrorRef} tabIndex={-1} className="focus-target">
              <Alert tone="error" title="La demande n'a pas été envoyée.">
                <p>{errorMessage(submit.error)}</p>
              </Alert>
            </div>
          )}

          {leaveTypes.isPending && <LoadingState label="Chargement des types de congé…" />}
          {leaveTypes.isError && (
            <ErrorState error={leaveTypes.error} title="Impossible de charger les types de congé." onRetry={() => void leaveTypes.refetch()} />
          )}
          {leaveTypes.isSuccess && leaveTypes.data.length === 0 && (
            <EmptyState title="Aucun type de congé n'est disponible.">
              <p>Contactez le service RH.</p>
            </EmptyState>
          )}

          {leaveTypes.isSuccess && leaveTypes.data.length > 0 && (
            <form className="form" onSubmit={handleSubmit} noValidate aria-describedby="request-required-note">
              <p id="request-required-note" className="field__hint">
                Les champs marqués <span aria-hidden="true">*</span>
                <span className="sr-only">d'un astérisque</span> sont obligatoires.
              </p>

              <div className="field">
                <label htmlFor="request-leaveTypeId">
                  Type de congé <span aria-hidden="true">*</span>
                </label>
                <select
                  id="request-leaveTypeId"
                  required
                  value={form.leaveTypeId}
                  onChange={(event) => update('leaveTypeId', event.target.value)}
                  aria-invalid={errors.leaveTypeId ? true : undefined}
                  aria-describedby={describedBy('leaveTypeId', selectedType ? 'request-type-hint' : '')}
                >
                  <option value="">Choisir un type…</option>
                  {leaveTypes.data.map((type) => (
                    <option key={type.id} value={type.id}>
                      {type.label}
                    </option>
                  ))}
                </select>
                {selectedType && (
                  <p id="request-type-hint" className="field__hint">
                    <TypeHint type={selectedType} balances={balances.data} />
                  </p>
                )}
                {errors.leaveTypeId && (
                  <p id="request-leaveTypeId-error" className="field__error">
                    {errors.leaveTypeId}
                  </p>
                )}
              </div>

              <div className="field-row">
                <div className="field">
                  <label htmlFor="request-startDate">
                    Premier jour d'absence <span aria-hidden="true">*</span>
                  </label>
                  <input
                    id="request-startDate"
                    type="date"
                    required
                    value={form.startDate}
                    onChange={(event) => update('startDate', event.target.value)}
                    aria-invalid={errors.startDate ? true : undefined}
                    aria-describedby={describedBy('startDate')}
                  />
                  {errors.startDate && (
                    <p id="request-startDate-error" className="field__error">
                      {errors.startDate}
                    </p>
                  )}
                </div>
                <div className="field">
                  <label htmlFor="request-endDate">
                    Dernier jour d'absence <span aria-hidden="true">*</span>
                  </label>
                  <input
                    id="request-endDate"
                    type="date"
                    required
                    min={isIsoDate(form.startDate) ? form.startDate : undefined}
                    value={form.endDate}
                    onChange={(event) => update('endDate', event.target.value)}
                    aria-invalid={errors.endDate ? true : undefined}
                    aria-describedby={describedBy('endDate', 'request-endDate-hint')}
                  />
                  <p id="request-endDate-hint" className="field__hint">
                    Inclus. Week-ends et jours fériés ne sont pas décomptés.
                  </p>
                  {errors.endDate && (
                    <p id="request-endDate-error" className="field__error">
                      {errors.endDate}
                    </p>
                  )}
                </div>
              </div>

              <SimulationPanel
                hasInput={simulationKey(form) !== ''}
                isWaiting={simulationKey(form) !== debouncedKey || simulation.isFetching}
                simulation={simulation.data}
                error={simulation.error}
                tracked={selectedType?.isTracked ?? false}
              />

              <div className="field">
                <label htmlFor="request-comment">Commentaire (facultatif)</label>
                <textarea
                  id="request-comment"
                  rows={3}
                  maxLength={COMMENT_MAX}
                  value={form.comment}
                  onChange={(event) => update('comment', event.target.value)}
                  aria-invalid={errors.comment ? true : undefined}
                  aria-describedby={describedBy('comment', 'request-comment-warning', 'request-comment-count')}
                />
                <p id="request-comment-warning" className="field__hint field__hint--warning">
                  N'indiquez aucune information médicale.
                </p>
                <p id="request-comment-count" className="field__hint field__counter">
                  {form.comment.length} / {COMMENT_MAX} caractères
                </p>
                {errors.comment && (
                  <p id="request-comment-error" className="field__error">
                    {errors.comment}
                  </p>
                )}
              </div>

              <div className="form__actions">
                <button type="submit" className="button button--primary" disabled={submit.isPending}>
                  {submit.isPending && <Spinner />}
                  {submit.isPending ? 'Envoi en cours…' : 'Envoyer la demande'}
                </button>
              </div>
            </form>
          )}
        </section>

        <aside className="card" aria-labelledby="balances-title">
          <h2 id="balances-title">Mes soldes</h2>
          <BalancesList query={balances} />
        </aside>
      </div>
    </div>
  );
}

function TypeHint({ type, balances }: { type: LeaveType; balances: Balance[] | undefined }) {
  if (!type.isTracked) {
    return <>Ce type n'est pas soumis à décompte de solde.</>;
  }
  const balance = balances?.find((item) => item.leaveType.id === type.id);
  return balance ? <>Disponible : {formatDays(balance.available)}.</> : <>Type soumis à décompte de solde.</>;
}

interface SimulationPanelProps {
  hasInput: boolean;
  isWaiting: boolean;
  simulation: Simulation | undefined;
  error: unknown;
  tracked: boolean;
}

/** Résultat de la simulation, annoncé poliment à chaque mise à jour. */
function SimulationPanel({ hasInput, isWaiting, simulation, error, tracked }: SimulationPanelProps) {
  let content;
  if (!hasInput) {
    content = <p className="muted">Choisissez un type et des dates pour voir le nombre de jours décomptés.</p>;
  } else if (isWaiting) {
    content = (
      <p className="simulation__loading">
        <Spinner /> Calcul en cours…
      </p>
    );
  } else if (error) {
    const blocking = error instanceof ApiError && error.status === 422;
    content = (
      <p className={blocking ? 'simulation__problem' : undefined}>
        <strong>{blocking ? 'Demande impossible : ' : 'Simulation indisponible : '}</strong>
        {errorMessage(error)}
      </p>
    );
  } else if (simulation) {
    content = (
      <>
        <p className="simulation__days">
          <strong>{formatDays(simulation.daysCount)}</strong> ouvré{simulation.daysCount > 1 ? 's' : ''} décompté
          {simulation.daysCount > 1 ? 's' : ''}
        </p>
        {simulation.workingDays.length > 0 && simulation.workingDays.length <= 10 && (
          <p className="muted">{simulation.workingDays.map((day) => formatDate(day, 'short')).join(', ')}</p>
        )}
        {simulation.balance ? (
          <dl className="simulation__balance">
            <div>
              <dt>Disponible avant</dt>
              <dd>{formatDays(simulation.balance.availableBefore)}</dd>
            </div>
            <div>
              <dt>Disponible après</dt>
              <dd>{formatDays(simulation.balance.availableAfter)}</dd>
            </div>
          </dl>
        ) : (
          !tracked && <p className="muted">Aucun solde n'est débité pour ce type.</p>
        )}
        {simulation.blockingReasons.length > 0 && (
          <div className="simulation__problem">
            <p>
              <strong>Cette demande sera refusée à l'envoi :</strong>
            </p>
            <ul>
              {simulation.blockingReasons.map((reason) => (
                <li key={reason}>{blockingReasonLabel(reason)}</li>
              ))}
            </ul>
          </div>
        )}
      </>
    );
  }

  return (
    <section className="simulation" aria-labelledby="simulation-title">
      <h3 id="simulation-title" className="simulation__title">
        Estimation
      </h3>
      <div aria-live="polite" aria-atomic="true">
        {content}
      </div>
    </section>
  );
}

function BalancesList({ query }: { query: UseQueryResult<Balance[]> }) {
  if (query.isPending) {
    return <LoadingState label="Chargement des soldes…" />;
  }
  if (query.isError) {
    return <ErrorState error={query.error} title="Impossible de charger vos soldes." onRetry={() => void query.refetch()} />;
  }
  if (query.data.length === 0) {
    return <EmptyState title="Aucun solde à afficher." />;
  }
  return (
    <ul className="balances">
      {query.data.map((balance) => (
        <li key={balance.leaveType.id} className="balance">
          <p className="balance__label">{balance.leaveType.label}</p>
          <p className="balance__available">
            <span className="balance__value">{formatDays(balance.available)}</span> disponible{balance.available > 1 ? 's' : ''}
          </p>
          <dl className="balance__details">
            <div>
              <dt>Solde</dt>
              <dd>{formatDays(balance.amount)}</dd>
            </div>
            <div>
              <dt>En attente</dt>
              <dd>{formatDays(balance.pending)}</dd>
            </div>
          </dl>
        </li>
      ))}
    </ul>
  );
}
