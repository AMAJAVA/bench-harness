import { useCallback, useEffect, useMemo, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { teamsApi } from '../api/endpoints';
import type { CalendarEntry } from '../api/types';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { Spinner } from '../components/Spinner';
import {
  endOfMonth,
  formatMonthYear,
  formatRange,
  localTodayISO,
  mondayIndex,
  parseISODate,
  startOfMonth,
  toISODate,
} from '../utils/dates';

const WEEKDAYS = ['Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam', 'Dim'];

interface Cell {
  date: Date;
  iso: string;
  inMonth: boolean;
}

export function TeamCalendarPage() {
  const { user } = useAuth();
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [monthIndex, setMonthIndex] = useState(now.getMonth());
  const [entries, setEntries] = useState<CalendarEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const teamId = user?.teamId ?? null;
  const reload = useCallback(() => setReloadKey((k) => k + 1), []);

  useEffect(() => {
    if (!teamId) {
      setLoading(false);
      return;
    }
    let active = true;
    setLoading(true);
    setError(null);
    teamsApi
      .getCalendar(teamId, toISODate(startOfMonth(year, monthIndex)), toISODate(endOfMonth(year, monthIndex)))
      .then((data) => {
        if (!active) return;
        setEntries(data);
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
  }, [teamId, year, monthIndex, reloadKey]);

  const cells = useMemo<Cell[]>(() => {
    const first = startOfMonth(year, monthIndex);
    const offset = mondayIndex(first);
    const totalDays = endOfMonth(year, monthIndex).getUTCDate();
    const result: Cell[] = [];

    for (let i = 0; i < offset; i += 1) {
      const d = new Date(Date.UTC(year, monthIndex, 1 - (offset - i)));
      result.push({ date: d, iso: toISODate(d), inMonth: false });
    }
    for (let d = 1; d <= totalDays; d += 1) {
      const date = new Date(Date.UTC(year, monthIndex, d));
      result.push({ date, iso: toISODate(date), inMonth: true });
    }
    while (result.length % 7 !== 0) {
      const last = result[result.length - 1].date;
      const next = new Date(last);
      next.setUTCDate(next.getUTCDate() + 1);
      result.push({ date: next, iso: toISODate(next), inMonth: false });
    }
    return result;
  }, [year, monthIndex]);

  const entriesByDay = useMemo(() => {
    const map = new Map<string, CalendarEntry[]>();
    for (const entry of entries) {
      const start = parseISODate(entry.startDate);
      const end = parseISODate(entry.endDate);
      const cursor = new Date(start);
      while (cursor <= end) {
        const iso = toISODate(cursor);
        const list = map.get(iso) ?? [];
        list.push(entry);
        map.set(iso, list);
        cursor.setUTCDate(cursor.getUTCDate() + 1);
      }
    }
    return map;
  }, [entries]);

  function goPrevMonth() {
    if (monthIndex === 0) {
      setMonthIndex(11);
      setYear((y) => y - 1);
    } else {
      setMonthIndex((m) => m - 1);
    }
  }
  function goNextMonth() {
    if (monthIndex === 11) {
      setMonthIndex(0);
      setYear((y) => y + 1);
    } else {
      setMonthIndex((m) => m + 1);
    }
  }
  function goToday() {
    const d = new Date();
    setYear(d.getFullYear());
    setMonthIndex(d.getMonth());
  }

  if (!teamId) {
    return (
      <>
        <h1>Calendrier d'équipe</h1>
        <EmptyState title="Aucune équipe rattachée">
          Votre profil n'est rattaché à aucune équipe. Contactez votre administrateur RH.
        </EmptyState>
      </>
    );
  }

  const todayISO = localTodayISO();
  const monthLabel = formatMonthYear(year, monthIndex);

  return (
    <div className="stack-lg">
      <div>
        <h1>Calendrier d'équipe</h1>
        <p className="muted">Absences approuvées de votre équipe.</p>
      </div>

      <section className="card" aria-labelledby="calendar-title">
        <h2 id="calendar-title" className="hidden-visually">Calendrier des absences</h2>

        <div className="calendar-toolbar">
          <div className="row">
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={goPrevMonth}
              aria-label="Mois précédent"
            >
              ‹
            </button>
            <span className="calendar-month-label" aria-live="polite">{monthLabel}</span>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={goNextMonth}
              aria-label="Mois suivant"
            >
              ›
            </button>
          </div>
          <button type="button" className="btn btn-ghost btn-sm" onClick={goToday}>
            Aujourd'hui
          </button>
        </div>

        {loading && <Spinner label="Chargement du calendrier…" />}
        {error && <ErrorState error={error} onRetry={reload} />}

        {!loading && !error && (
          <>
            <div className="calendar-grid" role="grid" aria-label={`Calendrier de ${monthLabel}`}>
              {WEEKDAYS.map((w) => (
                <div key={w} className="calendar-weekday" role="columnheader">{w}</div>
              ))}
              {cells.map((cell) => {
                const dayEntries = entriesByDay.get(cell.iso) ?? [];
                const classes = ['calendar-cell'];
                if (!cell.inMonth) classes.push('outside');
                if (cell.iso === todayISO) classes.push('today');

                return (
                  <div
                    key={cell.iso}
                    className={classes.join(' ')}
                    role="gridcell"
                    aria-label={`${cell.date.getUTCDate()} — ${dayEntries.length} absence(s)`}
                  >
                    <span className="calendar-day-number" aria-hidden="true">
                      {cell.date.getUTCDate()}
                    </span>
                    {dayEntries.slice(0, 2).map((entry) => {
                      const initials = `${entry.employeeFirstName.charAt(0)}${entry.employeeLastName.charAt(0)}`;
                      const full = `${entry.employeeFirstName} ${entry.employeeLastName} — ${entry.leaveTypeLabel}`;
                      return (
                        <span key={entry.leaveRequestId} className="calendar-chip" title={full}>
                          <span aria-hidden="true">{initials}</span>
                          <span className="hidden-visually">{full}</span>
                        </span>
                      );
                    })}
                    {dayEntries.length > 2 && (
                      <span className="calendar-chip more" aria-label={`${dayEntries.length - 2} absences supplémentaires`}>
                        +{dayEntries.length - 2}
                      </span>
                    )}
                  </div>
                );
              })}
            </div>

            <div className="stack" style={{ marginTop: '1.5rem' }}>
              <h3>Absences du mois</h3>
              {entries.length === 0 ? (
                <EmptyState title="Aucune absence approuvée sur la période">
                  Le calendrier affiche uniquement les absences approuvées.
                </EmptyState>
              ) : (
                <ul style={{ listStyle: 'none', padding: 0, margin: 0 }} className="stack">
                  {entries.map((entry) => (
                    <li
                      key={entry.leaveRequestId}
                      className="card"
                      style={{ padding: '0.75rem 1rem', margin: 0 }}
                    >
                      <div className="row">
                        <strong>
                          {entry.employeeFirstName} {entry.employeeLastName}
                        </strong>
                        <span className="muted" aria-hidden="true">—</span>
                        <span>{entry.leaveTypeLabel}</span>
                        <span className="spacer" />
                        <span className="text-sm muted">
                          {formatRange(entry.startDate, entry.endDate)}
                        </span>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </>
        )}
      </section>
    </div>
  );
}
