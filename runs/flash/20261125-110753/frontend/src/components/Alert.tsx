import type { ReactNode } from 'react';

type Tone = 'info' | 'success' | 'warning' | 'error';

const ICONS: Record<Tone, string> = {
  info: 'i',
  success: '✓',
  warning: '!',
  error: '!',
};

export function Alert({
  tone = 'info',
  title,
  children,
}: {
  tone?: Tone;
  title?: string;
  children?: ReactNode;
}) {
  const isUrgent = tone === 'error' || tone === 'warning';
  return (
    <div
      className={`alert alert-${tone}`}
      role={isUrgent ? 'alert' : 'status'}
      aria-live={isUrgent ? 'assertive' : 'polite'}
    >
      <span className="alert-icon" aria-hidden="true">{ICONS[tone]}</span>
      <div>
        {title && <div className="alert-title">{title}</div>}
        {children && <div className="alert-body">{children}</div>}
      </div>
    </div>
  );
}
