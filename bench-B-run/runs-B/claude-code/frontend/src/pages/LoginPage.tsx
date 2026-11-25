import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { errorMessage, fieldErrors } from '../api/errors';
import { useAuth } from '../auth/AuthContext';
import { Alert, Spinner } from '../components/Feedback';
import { usePageHeading } from '../lib/usePageHeading';

export function LoginPage() {
  const { session, login, notice } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const headingRef = usePageHeading('Connexion');

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<unknown>(null);

  if (session && !submitting) {
    return <Navigate to={session.mustChangePassword ? '/mot-de-passe' : '/'} replace />;
  }

  const from = (location.state as { from?: string } | null)?.from;
  const fields = fieldErrors(error);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const next = await login(email.trim(), password);
      navigate(next.mustChangePassword ? '/mot-de-passe' : (from ?? '/'), { replace: true });
    } catch (caught) {
      setError(caught);
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="card auth-card">
        <p className="brand brand--large">
          <span className="brand__mark" aria-hidden="true" />
          TeamLeave
        </p>
        <h1 ref={headingRef} tabIndex={-1}>
          Connexion
        </h1>
        {notice && !error && <Alert tone="info">{notice}</Alert>}
        {error !== null && <Alert tone="error">{errorMessage(error)}</Alert>}
        <form className="form" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="login-email">Adresse email</label>
            <input
              id="login-email"
              type="email"
              autoComplete="username"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              aria-invalid={fields.email ? true : undefined}
              aria-describedby={fields.email ? 'login-email-error' : undefined}
            />
            {fields.email && (
              <p id="login-email-error" className="field__error">
                {fields.email}
              </p>
            )}
          </div>
          <div className="field">
            <label htmlFor="login-password">Mot de passe</label>
            <input
              id="login-password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              aria-invalid={fields.password ? true : undefined}
              aria-describedby={fields.password ? 'login-password-error' : undefined}
            />
            {fields.password && (
              <p id="login-password-error" className="field__error">
                {fields.password}
              </p>
            )}
          </div>
          <button type="submit" className="button button--primary button--block" disabled={submitting}>
            {submitting && <Spinner />}
            {submitting ? 'Connexion…' : 'Se connecter'}
          </button>
        </form>
      </div>
    </div>
  );
}
