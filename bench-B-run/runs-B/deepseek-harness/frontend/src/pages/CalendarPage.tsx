import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api/endpoints';
import type { CalendarAbsence, LeaveTypeSummary } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import {
  WEEKDAY_LABELS,
  WEEKDAY_SHORT_LABELS,
  addDays,
  addMonths,
  dateParts,
  formatMonth,
  formatMediumDate,
  halfDayRangeLabel,
  isWeekend,
  monthBounds,
  monthOf,
  monthWeeks,
  todayInParis,
} from '../lib/dates';
import { describeError } from '../lib/errors';
import { usePageTitle } from '../hooks/usePageTitle';
import { PageHeader } from '../components/PageHeader';
import { TypeBadge } from '../components/Badges';
import {
  EmptyState,
  ErrorState,
  LoadingState,
  PermissionNotice,
} from '../components/Feedback';
import {
  CalendarIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  InboxIcon,
  LockIcon,
  RefreshIcon,
  UsersIcon,
} from '../components/icons';

interface DayEntry {
  employeeId: string;
  name: string;
  am: boolean;
  pm: boolean;
  leaveType?: LeaveTypeSummary;
}

function buildDayMap(
  absences: CalendarAbsence[],
  from: string,
  to: string,
): Map<string, DayEntry[]> {
  const byDay = new Map<string, Map<string, DayEntry>>();

  for (const absence of absences) {
    const start = absence.startDate > from ? absence.startDate : from;
    const end = absence.endDate < to ? absence.endDate : to;

    if (end < start) {
      continue;
    }

    let cursor = start;
    let guard = 0;
    while (cursor <= end && guard < 400) {
      if (!isWeekend(cursor)) {
        const coversMorning =
          cursor !== absence.startDate || absence.startHalfDay === 'AM';
        const coversAfternoon =
          cursor !== absence.endDate || absence.endHalfDay === 'PM';

        if (coversMorning || coversAfternoon) {
          let day = byDay.get(cursor);
          if (!day) {
            day = new Map<string, DayEntry>();
            byDay.set(cursor, day);
          }

          const existing = day.get(absence.employeeId);
          if (existing) {
            existing.am = existing.am || coversMorning;
            existing.pm = existing.pm || coversAfternoon;
            existing.leaveType = existing.leaveType ?? absence.leaveType;
          } else {
            day.set(absence.employeeId, {
              employeeId: absence.employeeId,
              name: absence.employeeDisplayName,
              am: coversMorning,
              pm: coversAfternoon,
              leaveType: absence.leaveType,
            });
          }
        }
      }

      cursor = addDays(cursor, 1);
      guard += 1;
    }
  }

  return new Map(
    [...byDay.entries()].map(([date, employees]) => [
      date,
      [...employees.values()].sort((a, b) => a.name.localeCompare(b.name, 'fr')),
    ]),
  );
}

function dayMoment(entry: DayEntry): string {
  if (entry.am && entry.pm) {
    return 'journée complète';
  }
  return entry.am ? 'matin' : 'après-midi';
}

function displayInitials(name: string): string {
  const parts = name.trim().split(/\s+/);
  const first = parts[0]?.charAt(0) ?? '';
  const last = parts.length > 1 ? (parts[parts.length - 1]?.charAt(0) ?? '') : '';
  return `${first}${last}`.toUpperCase();
}

export function CalendarPage() {
  usePageTitle('Calendrier d’équipe');
  const { user } = useAuth();
  const today = todayInParis();
  const initialMonth = monthOf(today);
  const [visibleMonth, setVisibleMonth] = useState(initialMonth);

  const { from, to } = monthBounds(
    visibleMonth.year,
    visibleMonth.monthIndex,
  );
  const teamId = user?.team?.id;

  const calendarQuery = useQuery({
    queryKey: ['calendar', teamId, from, to],
    queryFn: ({ signal }) =>
      api.getTeamCalendar(teamId as string, from, to, signal),
    enabled: Boolean(teamId),
  });

  const absences = calendarQuery.data?.absences ?? [];
  const weeks = useMemo(
    () => monthWeeks(visibleMonth.year, visibleMonth.monthIndex),
    [visibleMonth],
  );
  const dayMap = useMemo(
    () => buildDayMap(absences, from, to),
    [absences, from, to],
  );
  const sortedAbsences = useMemo(
    () =>
      [...absences].sort((a, b) => {
        if (a.startDate !== b.startDate) {
          return a.startDate.localeCompare(b.startDate);
        }
        return a.employeeDisplayName.localeCompare(b.employeeDisplayName, 'fr');
      }),
    [absences],
  );

  const isCurrentMonth =
    visibleMonth.year === initialMonth.year &&
    visibleMonth.monthIndex === initialMonth.monthIndex;

  const moveMonth = (delta: number) => {
    setVisibleMonth((current) => addMonths(current.year, current.monthIndex, delta));
  };

  if (!teamId) {
    return (
      <div className="page">
        <PageHeader
          eyebrow="Équipe"
          title="Calendrier d’équipe"
          description="Les absences approuvées de votre équipe, en lecture seule."
        />
        <PermissionNotice title="Aucune équipe rattachée">
          <p>
            Votre compte n’est rattaché à aucune équipe active. Contactez
            l’Admin RH pour corriger votre rattachement.
          </p>
        </PermissionNotice>
      </div>
    );
  }

  return (
    <div className="page">
      <PageHeader
        eyebrow="Équipe"
        title="Calendrier d’équipe"
        description={
          <>
            Absences approuvées de <strong>{user?.team?.name}</strong>. Les
            demandes en attente, refusées ou annulées n’apparaissent jamais.
          </>
        }
        actions={
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => void calendarQuery.refetch()}
            disabled={calendarQuery.isFetching}
          >
            <RefreshIcon size={18} />
            {calendarQuery.isFetching ? 'Actualisation…' : 'Actualiser'}
          </button>
        }
      />

      <section className="card calendar-card" aria-labelledby="calendar-title">
        <div className="calendar-toolbar">
          <div className="month-nav">
            <button
              type="button"
              className="icon-button"
              onClick={() => moveMonth(-1)}
              aria-label="Mois précédent"
            >
              <ChevronLeftIcon size={20} />
            </button>
            <h2 id="calendar-title" className="month-title" aria-live="polite">
              {formatMonth(visibleMonth.year, visibleMonth.monthIndex)}
            </h2>
            <button
              type="button"
              className="icon-button"
              onClick={() => moveMonth(1)}
              aria-label="Mois suivant"
            >
              <ChevronRightIcon size={20} />
            </button>
          </div>

          <div className="calendar-toolbar-actions">
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() => setVisibleMonth(initialMonth)}
              disabled={isCurrentMonth}
            >
              <CalendarIcon size={18} />
              Aujourd’hui
            </button>
            <span className="count-pill" aria-live="polite">
              {calendarQuery.isSuccess
                ? `${absences.length} absence${absences.length > 1 ? 's' : ''}`
                : '—'}
            </span>
          </div>
        </div>

        {calendarQuery.isPending ? (
          <LoadingState label="Chargement du calendrier…" rows={5} />
        ) : calendarQuery.isError ? (
          <ErrorState
            title="Calendrier indisponible"
            message={describeError(calendarQuery.error).message}
            code={describeError(calendarQuery.error).code}
            onRetry={() => void calendarQuery.refetch()}
          />
        ) : (
          <>
            <div className="calendar-scroll">
              <table className="calendar-table">
                <caption className="sr-only">
                  Calendrier des absences approuvées de l’équipe{' '}
                  {calendarQuery.data?.teamName ?? ''} pour{' '}
                  {formatMonth(visibleMonth.year, visibleMonth.monthIndex)}.
                  Chaque jour indique le nombre de personnes absentes ; la liste
                  détaillée suit le calendrier.
                </caption>
                <thead>
                  <tr>
                    {WEEKDAY_LABELS.map((label, index) => (
                      <th key={label} scope="col" className="calendar-weekday">
                        <span className="sr-only">{label}</span>
                        <span aria-hidden="true">{WEEKDAY_SHORT_LABELS[index]}</span>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {weeks.map((week) => (
                    <tr key={week[0]}>
                      {week.map((date) => {
                        const parts = dateParts(date);
                        const inMonth =
                          parts?.year === visibleMonth.year &&
                          parts.month - 1 === visibleMonth.monthIndex;
                        const entries = dayMap.get(date) ?? [];
                        const isToday = date === today && inMonth;
                        const isWeekendDay = isWeekend(date);

                        return (
                          <td
                            key={date}
                            className={`calendar-cell${inMonth ? '' : ' is-outside'}${isWeekendDay ? ' is-weekend' : ''}${isToday ? ' is-today' : ''}`}
                            aria-current={isToday ? 'date' : undefined}
                          >
                            <div className="calendar-cell-inner">
                              <span
                                className="calendar-day-number"
                                aria-hidden={inMonth ? undefined : true}
                              >
                                {parts?.day}
                                {isToday ? (
                                  <span className="sr-only">
                                    {' '}
                                    (aujourd’hui)
                                  </span>
                                ) : null}
                              </span>

                              {entries.length > 0 ? (
                                <>
                                  <span
                                    className="calendar-count"
                                    aria-hidden="true"
                                  >
                                    {entries.length}
                                  </span>
                                  <span className="sr-only">
                                    {entries
                                      .map(
                                        (entry) =>
                                          `${entry.name} : ${dayMoment(entry)}`,
                                      )
                                      .join(' ; ')}
                                  </span>
                                  <ul className="calendar-names" aria-hidden="true">
                                    {entries.slice(0, 2).map((entry) => (
                                      <li key={entry.employeeId} className="calendar-name">
                                        {entry.name}
                                      </li>
                                    ))}
                                    {entries.length > 2 ? (
                                      <li className="calendar-more">
                                        +{entries.length - 2}
                                      </li>
                                    ) : null}
                                  </ul>
                                </>
                              ) : null}
                            </div>
                          </td>
                        );
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="calendar-legend">
              <span className="legend-item">
                <span className="legend-swatch" aria-hidden="true" />
                Absence approuvée
              </span>
              <span className="legend-item">
                <span className="legend-today" aria-hidden="true" />
                Aujourd’hui
              </span>
              <span className="legend-note">
                Les week-ends sont affichés comme repères et ne portent aucune
                absence décomptée.
              </span>
            </div>
          </>
        )}
      </section>

      <section className="card" aria-labelledby="absence-list-title">
        <div className="card-header">
          <div>
            <h2 id="absence-list-title" className="card-title">
              Absences du mois
            </h2>
            <p className="card-subtitle">
              Lecture seule. Le type de congé n’est visible que de l’Admin RH et
              du manager direct de la personne concernée.
            </p>
          </div>
          <span className="count-pill" aria-hidden="true">
            {sortedAbsences.length}
          </span>
        </div>

        {calendarQuery.isPending ? (
          <LoadingState label="Chargement des absences…" rows={2} />
        ) : calendarQuery.isError ? (
          <p className="inline-empty">
            Les absences ne peuvent pas être affichées pour le moment.
          </p>
        ) : sortedAbsences.length === 0 ? (
          <EmptyState
            icon={<InboxIcon size={24} />}
            title="Aucune absence approuvée ce mois-ci"
            description={`Personne de l’équipe ${calendarQuery.data?.teamName ?? ''} n’a d’absence approuvée entre le ${formatMediumDate(from)} et le ${formatMediumDate(to)}.`}
            action={
              <Link to="/demandes/nouvelle" className="btn btn-secondary">
                Soumettre une demande
              </Link>
            }
          />
        ) : (
          <ul className="absence-list">
            {sortedAbsences.map((absence) => (
              <li
                key={`${absence.employeeId}-${absence.startDate}-${absence.startHalfDay}-${absence.endDate}-${absence.endHalfDay}`}
                className="absence-row"
              >
                <span className="avatar avatar-sm" aria-hidden="true">
                  {displayInitials(absence.employeeDisplayName)}
                </span>
                <div className="absence-main">
                  <div className="absence-top">
                    <strong className="absence-name">
                      {absence.employeeDisplayName}
                    </strong>
                    {absence.leaveType ? (
                      <TypeBadge
                        label={absence.leaveType.label}
                        color={absence.leaveType.color}
                      />
                    ) : (
                      <span className="privacy-badge">
                        <LockIcon size={12} />
                        Type non communiqué
                      </span>
                    )}
                  </div>
                  <p className="absence-dates">
                    {halfDayRangeLabel(
                      absence.startDate,
                      absence.startHalfDay,
                      absence.endDate,
                      absence.endHalfDay,
                    )}
                  </p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <p className="page-footnote">
        <UsersIcon size={16} />
        Le calendrier utilise votre équipe courante et se limite aux absences
        approuvées. Les jours fériés ne sont pas gérés en v1.
      </p>
    </div>
  );
}
