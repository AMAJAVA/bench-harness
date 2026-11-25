import { parseISODate, toISODate } from './dates';

/**
 * Aperçu côté client du calcul de jours ouvrés (RB-04).
 * Le backend reste la source de vérité : on n'utilise ce résultat que pour
 * l'affichage dans le formulaire.
 */
export function computeWorkingDays(
  startISO: string,
  endISO: string,
  holidayISO: Set<string> = new Set(),
): number {
  const start = parseISODate(startISO);
  const end = parseISODate(endISO);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return 0;
  if (start > end) return 0;

  let count = 0;
  const cursor = new Date(start);
  while (cursor <= end) {
    const dow = cursor.getUTCDay();
    const iso = toISODate(cursor);
    if (dow !== 0 && dow !== 6 && !holidayISO.has(iso)) {
      count += 1;
    }
    cursor.setUTCDate(cursor.getUTCDate() + 1);
  }
  return count;
}

=== FILE: src/pages/LoginPage.tsx
import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import { Alert } from '../components/Alert';

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/requests/new';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (!email.trim() || !password) {
      setError('Merci de saisir votre email et votre mot de passe.');
      return;
    }

    setSubmitting(true);
    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError('Email ou mot de passe incorrect.');
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Connexion impossible. Vérifiez votre connexion réseau.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="login-shell">
      <div className="login-card">
        <h1>TeamLeave</h1>
        <p className="login-subtitle">Connectez-vous pour gérer vos congés.</p>
        <form className="form" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="email">Email professionnel</label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="username"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              aria-describedby={error ? 'login-error' : undefined}
            />
          </div>
          <div className="field">
            <label htmlFor="password">Mot de passe</label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              aria-describedby={error ? 'login-error' : undefined}
            />
          </div>
          {error && (
            <div id="login-error">
              <Alert tone="error" title="Connexion impossible">{error}</Alert>
            </div>
          )}
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Connexion…' : 'Se connecter'}
          </button>
        </form>
      </div>
    </div>
  );
}

=== FILE: src/pages/SubmitLeaveRequestPage.tsx
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { ApiError } from '../api/client';
import { holidaysApi, leaveRequestsApi, leaveTypesApi, meApi } from '../api/endpoints';
import type { Holiday, LeaveBalance, LeaveRequest, LeaveType } from '../api/types';
import { Alert } from '../components/Alert';
import { EmptyState } from '../components/EmptyState';
import { ErrorState, describeError } from '../components/ErrorState';
import { OverlapBadge } from '../components/OverlapBadge';
import { Spinner } from '../components/Spinner';
import { StatusBadge } from '../components/StatusBadge';
import { formatDateTime, formatRange } from '../utils/dates';
import { computeWorkingDays } from '../utils/workingDays';

export function SubmitLeaveRequestPage() {
  const currentYear = new Date().getFullYear();

  const [loading, setLoading] = useState(true);
  const [initialLoadDone, setInitialLoadDone] = useState(false);
  const [loadError, setLoadError] = useState<unknown>(null);
  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [types, setTypes] = useState<LeaveType[]>([]);
  const [holidays, setHolidays] = useState<Holiday[]>([]);
  const [myRequests, setMyRequests] = useState<LeaveRequest[]>([]);
  const [reloadKey, setReloadKey] = useState(0);

  const [leaveTypeId, setLeaveTypeId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [lastCreated, setLastCreated] = useState<LeaveRequest | null>(null);

  const reload = useCallback(() => setReloadKey((k) => k + 1), []);

  // Année ciblée = année de la date de début (RB-05), sinon année courante.
  const targetYear = startDate ? Number(startDate.slice(0, 4)) : currentYear;

  useEffect(() => {
    let active = true;
    setLoading(true);
    setLoadError(null);

    Promise.all([
      meApi.getBalances(targetYear),
      leaveTypesApi.list(),
      holidaysApi.list(targetYear),
      meApi.getLeaveRequests({}),
    ])
      .then(([balancesData, typesData, holidaysData, requestsData]) => {
        if (!active) return;
        setBalances(balancesData);
        setTypes(typesData.filter((t) => t.isActive));
        setHolidays(holidaysData.filter((h) => h.isActive));
        setMyRequests(requestsData);
        setLoading(false);
        setInitialLoadDone(true);
      })
      .catch((err) => {
        if (!active) return;
        setLoadError(err);
        setLoading(false);
        setInitialLoadDone(true);
      });

    return () => {
      active = false;
    };
  }, [targetYear, reloadKey]);

  // Sélection par défaut du premier type actif.
  useEffect(() => {
    if (!leaveTypeId && types.length > 0) {
      setLeaveTypeId(types[0].id);
    }
  }, [types, leaveTypeId]);

  const holidaySet = useMemo(() => new Set(holidays.map((h) => h.date)), [holidays]);

  const workingDays = useMemo(() => {
    if (!startDate || !endDate) return 0;
    return computeWorkingDays(startDate, endDate, holidaySet);
  }, [startDate, endDate, holidaySet]);

  const selectedBalance = balances.find((b) => b.leaveTypeId === leaveTypeId) ?? null;

  const dateError = (() => {
    if (startDate && endDate && startDate > endDate) {
      return 'La date de début doit être antérieure ou égale à la date de fin.';
    }
    if (startDate && endDate && startDate.slice(0, 4) !== endDate.slice(0, 4)) {
      return 'Une demande ne peut pas couvrir deux années civiles.';
    }
    return null;
  })();

  const balanceError = (() => {
    if (!selectedBalance || !workingDays) return null;
    if (workingDays > selectedBalance.availableDays) {
      return `La durée (${workingDays} j) dépasse le solde disponible (${selectedBalance.availableDays} j).`;
    }
    return null;
  })();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    setLastCreated(null);

    if (!leaveTypeId) {
      setFormError('Sélectionnez un type de congé.');
      return;
    }
    if (!startDate || !endDate) {
      setFormError('Renseignez les dates de début et de fin.');
      return;
    }
    if (dateError) {
      setFormError(dateError);
      return;
    }
    if (balanceError) {
      setFormError(balanceError);
      return;
    }

    setSubmitting(true);
    try {
      const created = await leaveRequestsApi.create({
        leaveTypeId,
        startDate,
        endDate,
        comment: comment.trim() || undefined,
      });
      setLastCreated(created);
      setStartDate('');
      setEndDate('');
      setComment('');
      setMyRequests((prev) => [created, ...prev]);
      // Rafraîchit les soldes ciblés (nouvelle année éventuelle)
      meApi
        .getBalances(targetYear)
        .then((b) => setBalances(b))
        .catch(() => {
          /* silencieux : la liste est déjà à jour */
        });
    } catch (err) {
      if (err instanceof ApiError) {
        const fieldErrors = err.problem?.errors;
        if (fieldErrors && Object.keys(fieldErrors).length > 0) {
          const joined = Object.entries(fieldErrors)
            .map(([field, messages]) => `${field} : ${messages.join(' ')}`)
            .join(' · ');
          setFormError(`${describeError(err)} (${joined})`);
        } else {
          setFormError(describeError(err));
        }
      } else {
        setFormError(describeError(err));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (!initialLoadDone) {
    return <Spinner label="Chargement de vos données…" />;
  }
  if (loadError && balances.length === 0) {
    return <ErrorState error={loadError} onRetry={reload} />;
  }

  return (
    <div className="stack-lg">
      <div>
        <h1>Nouvelle demande de congé</h1>
        <p className="muted">
          Les jours ouvrés sont calculés automatiquement (hors week-ends et jours fériés).
        </p>
      </div>

      {loadError && <ErrorState error={loadError} onRetry={reload} />}

      {lastCreated && (
        <Alert tone="success" title="Demande soumise">
          Votre demande {formatRange(lastCreated.startDate, lastCreated.endDate)} est en attente de validation.
          {lastCreated.hasOverlapWarning && (
            <div style={{ marginTop: '0.5rem' }}>
              <OverlapBadge
                hasWarning={lastCreated.hasOverlapWarning}
                ratio={lastCreated.overlapRatio}
                details={lastCreated.overlapDetails}
              />
            </div>
          )}
        </Alert>
      )}

      <section className="card" aria-labelledby="balances-title">
        <h2 id="balances-title">Mes soldes {targetYear}</h2>
        {balances.length === 0 ? (
          <EmptyState title="Aucun solde configuré">
            Contactez votre administration RH pour l'ouverture de vos droits.
          </EmptyState>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <caption className="hidden-visually">
                Soldes de congés par type pour l'année {targetYear}
              </caption>
              <thead>
                <tr>
                  <th scope="col">Type</th>
                  <th scope="col">Acquis</th>
                  <th scope="col">Ajustements</th>
                  <th scope="col">Utilisés</th>
                  <th scope="col">Disponible</th>
                </tr>
              </thead>
              <tbody>
                {balances.map((b) => (
                  <tr key={b.leaveTypeId}>
                    <th scope="row">
                      {b.leaveTypeLabel}{' '}
                      <span className="muted text-xs">({b.leaveTypeCode})</span>
                    </th>
                    <td>{b.acquiredDays}</td>
                    <td>{b.adjustmentDays}</td>
                    <td>{b.usedDays}</td>
                    <td><strong>{b.availableDays}</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="card" aria-labelledby="form-title">
        <h2 id="form-title">Soumettre une demande</h2>
        <form className="form" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="leaveTypeId">Type de congé</label>
            <select
              id="leaveTypeId"
              required
              value={leaveTypeId}
              onChange={(e) => setLeaveTypeId(e.target.value)}
            >
              <option value="" disabled>Choisir un type…</option>
              {types.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.label} ({t.code})
                </option>
              ))}
            </select>
            {selectedBalance && (
              <span className="hint">
                Solde disponible : {selectedBalance.availableDays} jour
                {selectedBalance.availableDays > 1 ? 's' : ''}
              </span>
            )}
          </div>

          <div className="form-row cols-2">
            <div className="field">
              <label htmlFor="startDate">Date de début</label>
              <input
                id="startDate"
                type="date"
                required
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                aria-invalid={dateError ? 'true' : undefined}
              />
            </div>
            <div className="field">
              <label htmlFor="endDate">Date de fin</label>
              <input
                id="endDate"
                type="date"
                required
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                aria-invalid={dateError ? 'true' : undefined}
              />
            </div>
          </div>

          {dateError && (
            <Alert tone="error" title="Dates invalides">{dateError}</Alert>
          )}

          {startDate && endDate && !dateError && (
            <Alert
              tone="info"
              title={`${workingDays} jour${workingDays > 1 ? 's' : ''} ouvré${workingDays > 1 ? 's' : ''}`}
            >
              {workingDays === 0
                ? 'Aucun jour ouvré dans cette période.'
                : `Période ${formatRange(startDate, endDate)} (hors week-ends et jours fériés).`}
            </Alert>
          )}

          {balanceError && (
            <Alert tone="error" title="Solde insuffisant">{balanceError}</Alert>
          )}

          <div className="field">
            <label htmlFor="comment">Commentaire (facultatif)</label>
            <textarea
              id="comment"
              rows={3}
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="Ex. vacances d'été en famille"
            />
          </div>

          {formError && (
            <Alert tone="error" title="Impossible de soumettre la demande">
              {formError}
            </Alert>
          )}

          <div className="row">
            <button
              type="submit"
              className="btn btn-primary"
              disabled={submitting || Boolean(dateError) || Boolean(balanceError)}
            >
              {submitting ? 'Envoi…' : 'Soumettre la demande'}
            </button>
          </div>
        </form>
      </section>

      <section className="card" aria-labelledby="my-requests-title">
        <h2 id="my-requests-title">Mes demandes</h2>
        {myRequests.length === 0 ? (
          <EmptyState title="Aucune demande pour le moment">
            Vos demandes apparaîtront ici dès leur soumission.
          </EmptyState>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <caption className="hidden-visually">
                Historique de mes demandes de congé
              </caption>
              <thead>
                <tr>
                  <th scope="col">Période</th>
                  <th scope="col">Type</th>
                  <th scope="col">Jours</th>
                  <th scope="col">Statut</th>
                  <th scope="col">Soumise le</th>
                </tr>
              </thead>
              <tbody>
                {myRequests.map((r) => (
                  <tr key={r.id}>
                    <th scope="row">{formatRange(r.startDate, r.endDate)}</th>
                    <td>{r.leaveType.label}</td>
                    <td>{r.workingDays}</td>
                    <td><StatusBadge status={r.status} /></td>
                    <td>{formatDateTime(r.submittedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
