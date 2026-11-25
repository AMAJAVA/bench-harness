const PARIS_TIME_ZONE = 'Europe/Paris';
const UTC = 'UTC';

const fullDateFormatter = new Intl.DateTimeFormat('fr-FR', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  timeZone: UTC,
});

const mediumDateFormatter = new Intl.DateTimeFormat('fr-FR', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: UTC,
});

const shortDateFormatter = new Intl.DateTimeFormat('fr-FR', {
  day: 'numeric',
  month: 'short',
  timeZone: UTC,
});

const monthFormatter = new Intl.DateTimeFormat('fr-FR', {
  month: 'long',
  year: 'numeric',
  timeZone: UTC,
});

const monthShortFormatter = new Intl.DateTimeFormat('fr-FR', {
  month: 'short',
  year: 'numeric',
  timeZone: UTC,
});

const dateTimeFormatter = new Intl.DateTimeFormat('fr-FR', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: PARIS_TIME_ZONE,
});

const dayNumberFormatter = new Intl.NumberFormat('fr-FR', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 1,
});

const percentFormatter = new Intl.NumberFormat('fr-FR', {
  style: 'percent',
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
});

interface DateParts {
  year: number;
  month: number;
  day: number;
}

export const WEEKDAY_LABELS = [
  'Lundi',
  'Mardi',
  'Mercredi',
  'Jeudi',
  'Vendredi',
  'Samedi',
  'Dimanche',
] as const;

export const WEEKDAY_SHORT_LABELS = [
  'Lun.',
  'Mar.',
  'Mer.',
  'Jeu.',
  'Ven.',
  'Sam.',
  'Dim.',
] as const;

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

export function dateParts(isoDate: string): DateParts | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(Date.UTC(year, month - 1, day));

  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    return null;
  }

  return { year, month, day };
}

export function isValidIsoDate(value: string): boolean {
  return dateParts(value) !== null;
}

function toUtcDate(isoDate: string): Date {
  const parts = dateParts(isoDate);
  if (!parts) {
    throw new Error(`Date ISO invalide : ${isoDate}`);
  }
  return new Date(Date.UTC(parts.year, parts.month - 1, parts.day));
}

function fromUtcDate(date: Date): string {
  return `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}`;
}

export function todayInParis(now: Date = new Date()): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: PARIS_TIME_ZONE,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(now);

  const pick = (type: 'year' | 'month' | 'day'): string =>
    parts.find((part) => part.type === type)?.value ?? '';

  return `${pick('year')}-${pick('month')}-${pick('day')}`;
}

export function currentYearInParis(): number {
  return Number(todayInParis().slice(0, 4));
}

export function isWeekend(isoDate: string): boolean {
  const day = toUtcDate(isoDate).getUTCDay();
  return day === 0 || day === 6;
}

export function addDays(isoDate: string, amount: number): string {
  const date = toUtcDate(isoDate);
  date.setUTCDate(date.getUTCDate() + amount);
  return fromUtcDate(date);
}

export function addMonths(
  year: number,
  monthIndex: number,
  amount: number,
): { year: number; monthIndex: number } {
  const date = new Date(Date.UTC(year, monthIndex + amount, 1));
  return { year: date.getUTCFullYear(), monthIndex: date.getUTCMonth() };
}

export function monthBounds(
  year: number,
  monthIndex: number,
): { from: string; to: string } {
  const from = new Date(Date.UTC(year, monthIndex, 1));
  const to = new Date(Date.UTC(year, monthIndex + 1, 0));
  return { from: fromUtcDate(from), to: fromUtcDate(to) };
}

/** Grille du mois, du lundi au dimanche, semaines complètes. */
export function monthWeeks(year: number, monthIndex: number): string[][] {
  const first = new Date(Date.UTC(year, monthIndex, 1));
  const last = new Date(Date.UTC(year, monthIndex + 1, 0));
  const mondayOffset = (first.getUTCDay() + 6) % 7;
  const gridStart = new Date(first);
  gridStart.setUTCDate(gridStart.getUTCDate() - mondayOffset);

  const lastOffset = (last.getUTCDay() + 6) % 7;
  const gridEnd = new Date(last);
  gridEnd.setUTCDate(gridEnd.getUTCDate() + (6 - lastOffset));

  const weeks: string[][] = [];
  const cursor = new Date(gridStart);
  while (cursor <= gridEnd) {
    const week: string[] = [];
    for (let index = 0; index < 7; index += 1) {
      week.push(fromUtcDate(cursor));
      cursor.setUTCDate(cursor.getUTCDate() + 1);
    }
    weeks.push(week);
  }
  return weeks;
}

export function monthOf(isoDate: string): { year: number; monthIndex: number } {
  const parts = dateParts(isoDate);
  const fallback = new Date();
  return {
    year: parts?.year ?? fallback.getUTCFullYear(),
    monthIndex: (parts?.month ?? fallback.getUTCMonth() + 1) - 1,
  };
}

export function formatLongDate(isoDate: string): string {
  if (!isValidIsoDate(isoDate)) {
    return isoDate;
  }
  const label = fullDateFormatter.format(toUtcDate(isoDate));
  return label.charAt(0).toUpperCase() + label.slice(1);
}

export function formatMediumDate(isoDate: string): string {
  return isValidIsoDate(isoDate) ? mediumDateFormatter.format(toUtcDate(isoDate)) : isoDate;
}

export function formatShortDate(isoDate: string): string {
  return isValidIsoDate(isoDate) ? shortDateFormatter.format(toUtcDate(isoDate)) : isoDate;
}

export function formatMonth(year: number, monthIndex: number): string {
  const label = monthFormatter.format(new Date(Date.UTC(year, monthIndex, 1)));
  return label.charAt(0).toUpperCase() + label.slice(1);
}

export function formatMonthShort(year: number, monthIndex: number): string {
  return monthShortFormatter.format(new Date(Date.UTC(year, monthIndex, 1)));
}

export function formatDateTime(isoTimestamp: string): string {
  const date = new Date(isoTimestamp);
  if (Number.isNaN(date.getTime())) {
    return isoTimestamp;
  }
  return dateTimeFormatter.format(date);
}

export function formatDays(value: number): string {
  return `${dayNumberFormatter.format(value)} j`;
}

export function formatRate(value: number): string {
  return percentFormatter.format(value);
}

export function weekdayName(isoDate: string): string {
  const index = (toUtcDate(isoDate).getUTCDay() + 6) % 7;
  return WEEKDAY_LABELS[index] ?? '';
}

export function halfDayLabel(halfDay: 'AM' | 'PM'): string {
  return halfDay === 'AM' ? 'Matin' : 'Après-midi';
}

/**
 * Décompte indicatif des jours ouvrés, affiché à titre d'aide à la saisie.
 * Le décompte officiel (fuseau Europe/Paris, jours fériés inclus) est calculé
 * par le serveur et renvoyé dans `totalDays`.
 */
export function estimateWorkingDays(
  startDate: string,
  startHalfDay: 'AM' | 'PM',
  endDate: string,
  endHalfDay: 'AM' | 'PM',
): number | null {
  if (
    !isValidIsoDate(startDate) ||
    !isValidIsoDate(endDate) ||
    endDate < startDate
  ) {
    return null;
  }

  let halfDays = 0;
  let cursor = startDate;
  let guard = 0;
  while (cursor <= endDate && guard < 3660) {
    if (!isWeekend(cursor)) {
      const startSlot = cursor === startDate ? (startHalfDay === 'AM' ? 0 : 1) : 0;
      const endSlot = cursor === endDate ? (endHalfDay === 'AM' ? 1 : 2) : 2;
      halfDays += Math.max(0, endSlot - startSlot);
    }
    cursor = addDays(cursor, 1);
    guard += 1;
  }

  return halfDays / 2;
}

export function halfDayRangeLabel(
  startDate: string,
  startHalfDay: 'AM' | 'PM',
  endDate: string,
  endHalfDay: 'AM' | 'PM',
): string {
  if (startDate === endDate) {
    if (startHalfDay === 'AM' && endHalfDay === 'PM') {
      return 'Journée complète';
    }
    if (startHalfDay === endHalfDay) {
      return halfDayLabel(startHalfDay);
    }
  }

  return `Du ${formatMediumDate(startDate)} (${halfDayLabel(startHalfDay).toLowerCase()}) au ${formatMediumDate(endDate)} (${halfDayLabel(endHalfDay).toLowerCase()})`;
}

export function initials(firstName: string, lastName: string): string {
  const first = firstName.trim().charAt(0);
  const last = lastName.trim().charAt(0);
  return `${first}${last}`.toUpperCase();
}

export function fullName(person: { firstName: string; lastName: string }): string {
  return `${person.firstName} ${person.lastName}`.trim();
}
