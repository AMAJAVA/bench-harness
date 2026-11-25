import type { BlockingReason, LeaveRequestStatus, Rate, Role } from '../api/types';

const dayNumber = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 2 });

/** « 2,5 jours », « 1 jour », « 0 jour ». */
export function formatDays(value: number): string {
  return `${dayNumber.format(value)} ${Math.abs(value) > 1 ? 'jours' : 'jour'}`;
}

export const STATUS_LABELS: Record<LeaveRequestStatus, string> = {
  Pending: 'En attente',
  Approved: 'Approuvée',
  Rejected: 'Refusée',
  Cancelled: 'Annulée',
};

export const ROLE_LABELS: Record<Role, string> = {
  Employee: 'Employé',
  Manager: 'Manager',
  HrAdmin: 'Admin RH',
};

export const BLOCKING_REASON_LABELS: Record<BlockingReason, string> = {
  INSUFFICIENT_BALANCE: 'Solde disponible insuffisant pour cette période.',
  PAST_DATE_NOT_ALLOWED: "Ce type de congé n'autorise pas une date de début passée.",
  OVERLAPS_OWN_REQUEST: 'Vous avez déjà une demande en attente ou approuvée sur cette période.',
};

export function blockingReasonLabel(reason: string): string {
  return BLOCKING_REASON_LABELS[reason as BlockingReason] ?? reason;
}

/** « 2 absents sur 3 (67 %) ». */
export function formatRate(rate: Rate): string {
  return `${rate.absent} absent${rate.absent > 1 ? 's' : ''} sur ${rate.teamSize} (${rate.percent} %)`;
}
