import type { IsoDate, IsoDateTime } from '../api/types';

// Deux notions distinctes (GAP-09) :
// - date civile `YYYY-MM-DD` (dates de congé) : jamais convertie de fuseau, calculs faits en UTC ;
// - horodatage UTC (soumission, décision) : affiché en Europe/Paris.

const PARIS = 'Europe/Paris';

function toUtcDate(iso: IsoDate): Date {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(Date.UTC(y ?? 1970, (m ?? 1) - 1, d ?? 1));
}

function fromUtcDate(date: Date): IsoDate {
  return date.toISOString().slice(0, 10);
}

export function isIsoDate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(value) && fromUtcDate(toUtcDate(value)) === value;
}

export function addDays(iso: IsoDate, days: number): IsoDate {
  const date = toUtcDate(iso);
  date.setUTCDate(date.getUTCDate() + days);
  return fromUtcDate(date);
}

/** 0 = dimanche … 6 = samedi. */
export function weekday(iso: IsoDate): number {
  return toUtcDate(iso).getUTCDay();
}

export function isWeekend(iso: IsoDate): boolean {
  const day = weekday(iso);
  return day === 0 || day === 6;
}

/** Dates civiles de `from` à `to`, bornes incluses. */
export function eachDay(from: IsoDate, to: IsoDate): IsoDate[] {
  const days: IsoDate[] = [];
  for (let current = from; current <= to; current = addDays(current, 1)) {
    days.push(current);
  }
  return days;
}

/** Date du jour à Paris (RB-05 : « aujourd'hui » s'apprécie en Europe/Paris). */
export function todayInParis(): IsoDate {
  // Le format suédois produit directement `YYYY-MM-DD`.
  return new Intl.DateTimeFormat('sv-SE', { timeZone: PARIS, year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}

export interface MonthWindow {
  from: IsoDate;
  to: IsoDate;
  year: number;
}

/** Premier et dernier jour du mois contenant `iso`. */
export function monthWindow(iso: IsoDate): MonthWindow {
  const date = toUtcDate(iso);
  const year = date.getUTCFullYear();
  const month = date.getUTCMonth();
  return {
    from: fromUtcDate(new Date(Date.UTC(year, month, 1))),
    to: fromUtcDate(new Date(Date.UTC(year, month + 1, 0))),
    year,
  };
}

export function shiftMonth(iso: IsoDate, months: number): IsoDate {
  const date = toUtcDate(iso);
  return fromUtcDate(new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + months, 1)));
}

const formatters = {
  long: new Intl.DateTimeFormat('fr-FR', { timeZone: 'UTC', weekday: 'short', day: 'numeric', month: 'long', year: 'numeric' }),
  short: new Intl.DateTimeFormat('fr-FR', { timeZone: 'UTC', weekday: 'short', day: 'numeric', month: 'short' }),
  full: new Intl.DateTimeFormat('fr-FR', { timeZone: 'UTC', weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }),
  month: new Intl.DateTimeFormat('fr-FR', { timeZone: 'UTC', month: 'long', year: 'numeric' }),
  weekdayNarrow: new Intl.DateTimeFormat('fr-FR', { timeZone: 'UTC', weekday: 'narrow' }),
};

export type CivilFormat = keyof typeof formatters;

export function formatDate(iso: IsoDate, format: CivilFormat = 'long'): string {
  return formatters[format].format(toUtcDate(iso));
}

export function formatPeriod(start: IsoDate, end: IsoDate): string {
  return start === end ? `le ${formatDate(start)}` : `du ${formatDate(start)} au ${formatDate(end)}`;
}

/** Parse un horodatage de l'API ; ajoute `Z` si le serveur l'a omis (garde-fou, l'API renvoie de l'UTC). */
export function parseUtc(value: IsoDateTime): Date {
  const hasOffset = /([zZ]|[+-]\d{2}:?\d{2})$/.test(value);
  return new Date(hasOffset ? value : `${value}Z`);
}

const parisDateTime = new Intl.DateTimeFormat('fr-FR', { timeZone: PARIS, dateStyle: 'medium', timeStyle: 'short' });
const parisTime = new Intl.DateTimeFormat('fr-FR', { timeZone: PARIS, timeStyle: 'short' });

export function formatDateTimeParis(value: IsoDateTime, options: { timeOnly?: boolean } = {}): string {
  const date = parseUtc(value);
  return options.timeOnly ? parisTime.format(date) : parisDateTime.format(date);
}
