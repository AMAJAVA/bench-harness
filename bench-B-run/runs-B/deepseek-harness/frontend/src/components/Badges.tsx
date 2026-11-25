import type { LeaveStatus } from '../api/types';
import { AlertTriangleIcon } from './icons';

const STATUS_META: Record<
  LeaveStatus,
  { label: string; tone: 'pending' | 'approved' | 'rejected' | 'cancelled' }
> = {
  PENDING: { label: 'En attente', tone: 'pending' },
  APPROVED: { label: 'Approuvée', tone: 'approved' },
  REJECTED: { label: 'Refusée', tone: 'rejected' },
  CANCELLED: { label: 'Annulée', tone: 'cancelled' },
};

export function StatusBadge({ status }: { status: LeaveStatus }) {
  const meta = STATUS_META[status];
  return (
    <span className={`badge badge-${meta.tone}`}>
      <span className="badge-dot" aria-hidden="true" />
      {meta.label}
    </span>
  );
}

export function OverlapBadge({ compact = false }: { compact?: boolean }) {
  return (
    <span className="badge badge-warning">
      <AlertTriangleIcon size={14} />
      {compact ? 'Alerte' : 'Alerte capacité'}
    </span>
  );
}

export function TypeBadge({
  label,
  color,
}: {
  label: string;
  color?: string;
}) {
  return (
    <span className="type-badge">
      <span
        className="type-dot"
        aria-hidden="true"
        style={color ? { backgroundColor: color } : undefined}
      />
      {label}
    </span>
  );
}
