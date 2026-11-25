import type { ReactNode } from 'react';
import {
  AlertTriangleIcon,
  CheckCircleIcon,
  InfoIcon,
  LockIcon,
} from './icons';

export type AlertTone = 'info' | 'success' | 'warning' | 'danger';

interface AlertProps {
  tone?: AlertTone;
  title?: string;
  children?: ReactNode;
  role?: 'alert' | 'status' | 'note';
  actions?: ReactNode;
}

const ALERT_ICONS = {
  info: InfoIcon,
  success: CheckCircleIcon,
  warning: AlertTriangleIcon,
  danger: AlertTriangleIcon,
} as const;

export function Alert({
  tone = 'info',
  title,
  children,
  role,
  actions,
}: AlertProps) {
  const Icon = ALERT_ICONS[tone];
  const computedRole = role ?? (tone === 'danger' ? 'alert' : undefined);

  return (
    <div className={`alert alert-${tone}`} role={computedRole}>
      <span className="alert-icon" aria-hidden="true">
        <Icon size={20} />
      </span>
      <div className="alert-body">
        {title ? <p className="alert-title">{title}</p> : null}
        {children ? <div className="alert-text">{children}</div> : null}
        {actions ? <div className="alert-actions">{actions}</div> : null}
      </div>
    </div>
  );
}

export function Spinner({ label }: { label?: string }) {
  return (
    <span className="spinner" aria-hidden="true">
      {label ? <span className="sr-only">{label}</span> : null}
    </span>
  );
}

interface LoadingStateProps {
  label: string;
  rows?: number;
}

export function LoadingState({ label, rows = 3 }: LoadingStateProps) {
  return (
    <div className="loading-state" role="status" aria-live="polite">
      <span className="sr-only">{label}</span>
      <div className="skeleton-stack" aria-hidden="true">
        {Array.from({ length: rows }, (_, index) => (
          <div
            key={index}
            className="skeleton-line"
            style={{ width: `${88 - index * 12}%` }}
          />
        ))}
      </div>
    </div>
  );
}

interface EmptyStateProps {
  title: string;
  description?: ReactNode;
  icon?: ReactNode;
  action?: ReactNode;
  compact?: boolean;
}

export function EmptyState({
  title,
  description,
  icon,
  action,
  compact = false,
}: EmptyStateProps) {
  return (
    <div className={`empty-state${compact ? ' empty-state-compact' : ''}`}>
      <span className="empty-icon" aria-hidden="true">
        {icon ?? <CheckCircleIcon size={24} />}
      </span>
      <h2 className="empty-title">{title}</h2>
      {description ? <p className="empty-description">{description}</p> : null}
      {action ? <div className="empty-action">{action}</div> : null}
    </div>
  );
}

interface ErrorStateProps {
  title?: string;
  message: string;
  code?: string;
  onRetry?: () => void;
  retryLabel?: string;
}

export function ErrorState({
  title = 'Impossible de charger les données',
  message,
  code,
  onRetry,
  retryLabel = 'Réessayer',
}: ErrorStateProps) {
  return (
    <div className="state-card state-error" role="alert">
      <span className="state-icon" aria-hidden="true">
        <AlertTriangleIcon size={24} />
      </span>
      <div className="state-body">
        <h2 className="state-title">{title}</h2>
        <p className="state-message">{message}</p>
        {code ? <p className="state-code">Code : {code}</p> : null}
        {onRetry ? (
          <button type="button" className="btn btn-secondary" onClick={onRetry}>
            {retryLabel}
          </button>
        ) : null}
      </div>
    </div>
  );
}

export function FullPageLoader({ label }: { label: string }) {
  return (
    <div className="full-loader" role="status" aria-live="polite">
      <span className="full-loader-mark" aria-hidden="true">
        TL
      </span>
      <span className="full-loader-text">
        <span className="spinner" aria-hidden="true" />
        {label}
      </span>
    </div>
  );
}

export function PermissionNotice({
  title,
  children,
}: {
  title: string;
  children?: ReactNode;
}) {
  return (
    <div className="state-card state-locked">
      <span className="state-icon" aria-hidden="true">
        <LockIcon size={24} />
      </span>
      <div className="state-body">
        <h2 className="state-title">{title}</h2>
        {children ? <div className="state-message">{children}</div> : null}
      </div>
    </div>
  );
}
