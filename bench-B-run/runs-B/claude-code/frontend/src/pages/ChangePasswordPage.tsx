import { useState, type FormEvent } from 'react';
import { Navigate } from 'react-router';
import { api } from '../api/endpoints';
import { errorMessage, fieldErrors } from '../api/errors';
import { useAuth } from '../auth/AuthContext';
import { Alert, Spinner } from '../components/Feedback';
import { usePageHeading } from '../lib/usePageHeading';

const MIN_LENGTH = 12;
const MAX_LENGTH = 128;

/**
 * Changement du mot de passe temporaire (GAP-06). Indispensable pour utiliser l'application :
 * tant qu'il n'est pas fait, l'API répond 403 PASSWORD_CHANGE_REQUIRED.
 */
export function ChangePasswordPage() {
  const { session, logout } = useAuth();
  const headingRef = usePageHeading('Changer votre mot de passe');

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState<unknown>(null);
  const [submitting, setSubmitting] = useState(false);

  if (!session) {
    return <Navigate to="/connexion" replace />;
  }

  const fields = { ...fieldErrors(error), ...clientErrors };

  function validate(): Record<string, string> {
    const errors: Record<string, string> = {};
    if (!currentPassword) {
      errors.currentPassword = 'Saisissez votre mot de passe actuel.';
    }
    if (newPassword.length < MIN_LENGTH || newPassword.length > MAX_LENGTH) {
      errors.newPassword = `Le nouveau mot de passe doit contenir entre ${MIN_LENGTH} et ${MAX_LENGTH} caractères.`;
    } else if (newPassword === currentPassword) {
      errors.newPassword = "Le nouveau mot de passe doit être différent de l'actuel.";
    }
    if (confirmation !== newPassword) {
      errors.confirmation = 'La confirmation ne correspond pas au nouveau mot de passe.';
    }
    return errors;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    const errors = validate();
    setClientErrors(errors);
    const firstInvalid = Object.keys(errors)[0];
    if (firstInvalid) {
      document.getElementById(`pwd-${firstInvalid}`)?.focus();
      return;
    }
    setSubmitting(true);
    try {
      await api.changePassword({ currentPassword, newPassword });
      // Le tampon de sécurité est régénéré côté serveur : le jeton courant n'est plus valide.
      logout('Mot de passe modifié. Reconnectez-vous avec votre nouveau mot de passe.');
    } catch (caught) {
      setError(caught);
      setSubmitting(false);
    }
  }

  const describedBy = (name: string, hint?: string) =>
    [hint, fields[name] ? `pwd-${name}-error` : undefined].filter(Boolean).join(' ') || undefined;

  return (
    <div className="auth-page">
      <div className="card auth-card">
        <h1 ref={headingRef} tabIndex={-1}>
          Changer votre mot de passe
        </h1>
        <p className="muted">Votre mot de passe est temporaire. Choisissez-en un nouveau pour accéder à TeamLeave.</p>
        {error !== null && <Alert tone="error">{errorMessage(error)}</Alert>}
        <form className="form" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="pwd-currentPassword">Mot de passe actuel</label>
            <input
              id="pwd-currentPassword"
              type="password"
              autoComplete="current-password"
              required
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
              aria-invalid={fields.currentPassword ? true : undefined}
              aria-describedby={describedBy('currentPassword')}
            />
            {fields.currentPassword && (
              <p id="pwd-currentPassword-error" className="field__error">
                {fields.currentPassword}
              </p>
            )}
          </div>
          <div className="field">
            <label htmlFor="pwd-newPassword">Nouveau mot de passe</label>
            <input
              id="pwd-newPassword"
              type="password"
              autoComplete="new-password"
              required
              minLength={MIN_LENGTH}
              maxLength={MAX_LENGTH}
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              aria-invalid={fields.newPassword ? true : undefined}
              aria-describedby={describedBy('newPassword', 'pwd-newPassword-hint')}
            />
            <p id="pwd-newPassword-hint" className="field__hint">
              {MIN_LENGTH} caractères minimum, différent de votre adresse email.
            </p>
            {fields.newPassword && (
              <p id="pwd-newPassword-error" className="field__error">
                {fields.newPassword}
              </p>
            )}
          </div>
          <div className="field">
            <label htmlFor="pwd-confirmation">Confirmer le nouveau mot de passe</label>
            <input
              id="pwd-confirmation"
              type="password"
              autoComplete="new-password"
              required
              value={confirmation}
              onChange={(event) => setConfirmation(event.target.value)}
              aria-invalid={fields.confirmation ? true : undefined}
              aria-describedby={describedBy('confirmation')}
            />
            {fields.confirmation && (
              <p id="pwd-confirmation-error" className="field__error">
                {fields.confirmation}
              </p>
            )}
          </div>
          <button type="submit" className="button button--primary button--block" disabled={submitting}>
            {submitting && <Spinner />}
            {submitting ? 'Enregistrement…' : 'Enregistrer le mot de passe'}
          </button>
          <button type="button" className="button button--ghost button--block" onClick={() => logout()}>
            Se déconnecter
          </button>
        </form>
      </div>
    </div>
  );
}
