import type { Role, TimelineEvent } from '../api/types';

export function roleLabel(roles: readonly Role[]): string {
  const parts: string[] = [];
  if (roles.includes('ADMIN_RH')) {
    parts.push('Admin RH');
  }
  if (roles.includes('MANAGER')) {
    parts.push('Manager');
  }
  if (parts.length === 0) {
    parts.push('Employé');
  }
  return parts.join(' · ');
}

export function timelineLabel(event: TimelineEvent['event']): string {
  switch (event) {
    case 'SUBMITTED':
      return 'Demande soumise';
    case 'APPROVED':
      return 'Demande approuvée';
    case 'REJECTED':
      return 'Demande refusée';
    case 'CANCELLED':
      return 'Demande annulée';
    default:
      return event;
  }
}

export function cancellationReasonLabel(reason?: string): string {
  switch (reason) {
    case 'EMPLOYEE_CANCELLATION':
      return 'Annulation par le titulaire';
    case 'ACCOUNT_DEACTIVATION':
      return 'Annulation automatique liée à la désactivation du compte';
    default:
      return reason ?? 'Annulation';
  }
}
