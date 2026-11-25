import { useMemo, useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { api, queryKeys } from '../api/endpoints';
import type { CalendarAbsence, CalendarScope, Holiday, IsoDate, PersonRef } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Alert, EmptyState, ErrorState, LoadingState } from '../components/Feedback';
import { eachDay, formatDate, formatPeriod, isWeekend, monthWindow, shiftMonth, todayInParis } from '../lib/dates';
import { usePageHeading } from '../lib/usePageHeading';

type ViewMode = 'grid' | 'list';

const SCOPE_LABELS: Record<CalendarScope, string> = {
  team: 'Mon équipe',
  reports: 'Mes collaborateurs directs',
  all: "Toute l'entreprise",
};

const SCOPE_HINTS: Record<CalendarScope, string> = {
  team: "Collègues rattachés au même manager que vous, vous compris. Le type d'absence n'est pas communiqué.",
  reports: 'Vos subordonnés directs, avec le type de congé.',
  all: 'Tous les salariés, avec le type de congé.',
};

/** Code court affiché dans la grille ; « ABS » quand le type n'est pas communiqué (portée équipe, RB-23). */
function absenceCode(absence: CalendarAbsence): string {
  return absence.leaveType?.code ?? 'ABS';
}

function absenceLabel(absence: CalendarAbsence): string {
  return absence.leaveType?.label ?? 'Absent';
}

interface PersonRow {
  person: PersonRef;
  absences: CalendarAbsence[];
}

function groupByPerson(absences: CalendarAbsence[]): PersonRow[] {
  const rows = new Map<string, PersonRow>();
  for (const absence of absences) {
    const row = rows.get(absence.user.id) ?? { person: absence.user, absences: [] };
    row.absences.push(absence);
    rows.set(absence.user.id, row);
  }
  return [...rows.values()].sort((a, b) => a.person.fullName.localeCompare(b.person.fullName, 'fr'));
}

function initialView(): ViewMode {
  return typeof window !== 'undefined' && window.matchMedia('(min-width: 48rem)').matches ? 'grid' : 'list';
}

/** Écran 2 : calendrier d'équipe des absences approuvées (US-07, US-12, US-19 ; ENF-A3). */
export function CalendarPage() {
  const { hasRole } = useAuth();
  const headingRef = usePageHeading("Calendrier d'équipe");

  const scopes: CalendarScope[] = ['team'];
  if (hasRole('Manager')) scopes.push('reports');
  if (hasRole('HrAdmin')) scopes.push('all');

  const [scope, setScope] = useState<CalendarScope>(hasRole('Manager') ? 'reports' : 'team');
  const [monthStart, setMonthStart] = useState<IsoDate>(() => monthWindow(todayInParis()).from);
  const [view, setView] = useState<ViewMode>(initialView);

  const today = todayInParis();
  const { from, to, year } = monthWindow(monthStart);

  const calendar = useQuery({
    queryKey: queryKeys.calendar(from, to, scope),
    queryFn: ({ signal }) => api.getCalendar(from, to, scope, signal),
    placeholderData: keepPreviousData,
  });
  const holidays = useQuery({
    queryKey: queryKeys.holidays(year),
    queryFn: ({ signal }) => api.getHolidays(year, signal),
    staleTime: 60 * 60_000,
  });

  const days = useMemo(() => eachDay(from, to), [from, to]);
  const holidayByDate = useMemo(() => {
    const map = new Map<IsoDate, Holiday>();
    for (const holiday of holidays.data ?? []) {
      if (holiday.date >= from && holiday.date <= to) map.set(holiday.date, holiday);
    }
    return map;
  }, [holidays.data, from, to]);

  // Les données « précédentes » restent affichées pendant le chargement d'un autre mois : on ne s'en sert que si elles correspondent.
  const data = calendar.data && calendar.data.from === from && calendar.data.scope === scope ? calendar.data : undefined;
  const rows = useMemo(() => groupByPerson(data?.absences ?? []), [data]);
  const monthLabel = formatDate(from, 'month');

  return (
    <div className="page">
      <header className="page__header">
        <h1 ref={headingRef} tabIndex={-1}>
          Calendrier d'équipe
        </h1>
        <p className="muted">Absences approuvées uniquement. Les demandes en attente n'apparaissent pas.</p>
      </header>

      <section className="card toolbar" aria-label="Options d'affichage">
        {scopes.length > 1 ? (
          <fieldset className="segmented">
            <legend>Personnes affichées</legend>
            <div className="segmented__options">
              {scopes.map((option) => (
                <label key={option} className="segmented__option">
                  <input
                    type="radio"
                    name="calendar-scope"
                    value={option}
                    checked={scope === option}
                    onChange={() => setScope(option)}
                  />
                  <span>{SCOPE_LABELS[option]}</span>
                </label>
              ))}
            </div>
          </fieldset>
        ) : (
          <p className="toolbar__scope">
            <strong>{SCOPE_LABELS[scope]}</strong>
          </p>
        )}
        <p className="field__hint toolbar__hint">{SCOPE_HINTS[scope]}</p>

        <div className="toolbar__row">
          <div className="month-nav">
            <button type="button" className="button button--secondary" onClick={() => setMonthStart(shiftMonth(monthStart, -1))}>
              <span aria-hidden="true">←</span>
              <span className="sr-only">Mois précédent</span>
            </button>
            <h2 className="month-nav__label" aria-live="polite">
              {monthLabel}
            </h2>
            <button type="button" className="button button--secondary" onClick={() => setMonthStart(shiftMonth(monthStart, 1))}>
              <span aria-hidden="true">→</span>
              <span className="sr-only">Mois suivant</span>
            </button>
            <button
              type="button"
              className="button button--ghost"
              onClick={() => setMonthStart(monthWindow(today).from)}
              disabled={monthStart === monthWindow(today).from}
            >
              Mois en cours
            </button>
          </div>

          <div className="view-toggle" role="group" aria-label="Mode d'affichage">
            <button type="button" className="button button--toggle" aria-pressed={view === 'grid'} onClick={() => setView('grid')}>
              Grille
            </button>
            <button type="button" className="button button--toggle" aria-pressed={view === 'list'} onClick={() => setView('list')}>
              Liste
            </button>
          </div>
        </div>
      </section>

      <section className="card" aria-labelledby="calendar-results-title" aria-busy={calendar.isFetching}>
        <h2 id="calendar-results-title" className="sr-only">
          Absences de {monthLabel}
        </h2>

        {holidays.isError && (
          <Alert tone="warning">
            <p>Les jours fériés n'ont pas pu être chargés : ils ne sont pas signalés ci-dessous.</p>
          </Alert>
        )}

        {calendar.isError && !data && (
          <ErrorState error={calendar.error} title="Impossible de charger le calendrier." onRetry={() => void calendar.refetch()} />
        )}
        {!data && !calendar.isError && <LoadingState label={`Chargement des absences de ${monthLabel}…`} />}

        {data && rows.length === 0 && (
          <EmptyState title={`Aucune absence approuvée en ${monthLabel}.`}>
            <p className="muted">{SCOPE_LABELS[scope]} : personne n'est absent sur cette période.</p>
          </EmptyState>
        )}

        {data && rows.length > 0 && view === 'grid' && (
          <CalendarGrid rows={rows} days={days} holidays={holidayByDate} today={today} monthLabel={monthLabel} />
        )}
        {data && rows.length > 0 && view === 'list' && <AbsenceList absences={data.absences} from={from} to={to} />}

        {holidayByDate.size > 0 && (
          <p className="holidays-note">
            <strong>Jours fériés ce mois-ci :</strong>{' '}
            {[...holidayByDate.values()].map((holiday) => `${formatDate(holiday.date, 'short')} (${holiday.label})`).join(', ')}.
          </p>
        )}
      </section>
    </div>
  );
}

interface CalendarGridProps {
  rows: PersonRow[];
  days: IsoDate[];
  holidays: Map<IsoDate, Holiday>;
  today: IsoDate;
  monthLabel: string;
}

function CalendarGrid({ rows, days, holidays, today, monthLabel }: CalendarGridProps) {
  return (
    <>
      <div className="calendar-scroll" role="region" aria-labelledby="calendar-caption" tabIndex={0}>
        <table className="calendar-grid">
          <caption id="calendar-caption" className="sr-only">
            Absences approuvées en {monthLabel} : une ligne par personne, une colonne par jour.
          </caption>
          <thead>
            <tr>
              <th scope="col" className="calendar-grid__person">
                Personne
              </th>
              {days.map((day) => {
                const holiday = holidays.get(day);
                const off = isWeekend(day) || holiday !== undefined;
                const classes = ['calendar-grid__day', off ? 'is-off' : '', day === today ? 'is-today' : ''].filter(Boolean).join(' ');
                return (
                  <th key={day} scope="col" className={classes} title={holiday ? holiday.label : undefined}>
                    <span aria-hidden="true" className="calendar-grid__weekday">
                      {formatDate(day, 'weekdayNarrow')}
                    </span>
                    <span aria-hidden="true">{Number(day.slice(8))}</span>
                    <span className="sr-only">
                      {formatDate(day, 'full')}
                      {holiday ? `, férié (${holiday.label})` : isWeekend(day) ? ', week-end' : ''}
                      {day === today ? ", aujourd'hui" : ''}
                    </span>
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.person.id}>
                <th scope="row" className="calendar-grid__person">
                  {row.person.fullName}
                </th>
                {days.map((day) => {
                  const absence = row.absences.find((item) => item.startDate <= day && day <= item.endDate);
                  const off = isWeekend(day) || holidays.has(day);
                  const classes = [off ? 'is-off' : '', day === today ? 'is-today' : '', absence ? 'is-absent' : ''].filter(Boolean).join(' ');
                  return (
                    <td key={day} className={classes || undefined} title={absence ? `${absenceLabel(absence)} ${formatPeriod(absence.startDate, absence.endDate)}` : undefined}>
                      {absence && (
                        <>
                          <span aria-hidden="true" className="absence-chip">
                            {absenceCode(absence)}
                          </span>
                          <span className="sr-only">{absenceLabel(absence)}</span>
                        </>
                      )}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <ul className="legend" aria-label="Légende">
        <li>
          <span className="absence-chip" aria-hidden="true">
            CP
          </span>{' '}
          Absence : code du type de congé (« ABS » quand le type n'est pas communiqué)
        </li>
        <li>
          <span className="legend__swatch legend__swatch--off" aria-hidden="true" /> Colonne hachurée : week-end ou jour férié
        </li>
        <li>
          <span className="legend__swatch legend__swatch--today" aria-hidden="true" /> Colonne encadrée : aujourd'hui
        </li>
      </ul>
    </>
  );
}

/** Vue alternative en liste (ENF-A3), lisible sur mobile et au lecteur d'écran. */
function AbsenceList({ absences, from, to }: { absences: CalendarAbsence[]; from: IsoDate; to: IsoDate }) {
  const sorted = [...absences].sort(
    (a, b) => a.startDate.localeCompare(b.startDate) || a.user.fullName.localeCompare(b.user.fullName, 'fr'),
  );
  return (
    <ul className="absence-list">
      {sorted.map((absence) => {
        const startsBefore = absence.startDate < from;
        const endsAfter = absence.endDate > to;
        return (
          <li key={`${absence.user.id}-${absence.startDate}`} className="absence-list__item">
            <p className="absence-list__name">{absence.user.fullName}</p>
            <p>
              <span className="badge">{absenceLabel(absence)}</span> {formatPeriod(absence.startDate, absence.endDate)}
            </p>
            {(startsBefore || endsAfter) && (
              <p className="field__hint">
                {startsBefore && endsAfter
                  ? 'Absence commencée avant ce mois et qui se poursuit après.'
                  : startsBefore
                    ? 'Absence commencée avant ce mois.'
                    : 'Absence qui se poursuit le mois suivant.'}
              </p>
            )}
          </li>
        );
      })}
    </ul>
  );
}
