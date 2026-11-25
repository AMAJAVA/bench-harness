const FR_DATE = new Intl.DateTimeFormat('fr-FR', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
});

const FR_MONTH = new Intl.DateTimeFormat('fr-FR', {
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
});

const FR_DATETIME = new Intl.DateTimeFormat('fr-FR', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

export function parseISODate(value: string): Date {
  return new Date(`${value}T00:00:00Z`);
}

export function toISODate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function localTodayISO(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function formatDate(value: string | Date): string {
  const d = typeof value === 'string' ? parseISODate(value) : value;
  return FR_DATE.format(d);
}

export function formatDateTime(value: string | Date): string {
  const d = typeof value === 'string' ? new Date(value) : value;
  return FR_DATETIME.format(d);
}

export function formatMonthYear(year: number, monthIndex0: number): string {
  return FR_MONTH.format(new Date(Date.UTC(year, monthIndex0, 1)));
}

export function formatRange(startISO: string, endISO: string): string {
  if (startISO === endISO) return formatDate(startISO);
  return `du ${formatDate(startISO)} au ${formatDate(endISO)}`;
}

export function startOfMonth(year: number, monthIndex0: number): Date {
  return new Date(Date.UTC(year, monthIndex0, 1));
}

export function endOfMonth(year: number, monthIndex0: number): Date {
  return new Date(Date.UTC(year, monthIndex0 + 1, 0));
}

export function mondayIndex(date: Date): number {
  // 0 = lundi … 6 = dimanche (convention FR)
  return (date.getUTCDay() + 6) % 7;
}
