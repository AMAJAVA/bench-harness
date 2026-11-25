import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import { describeError } from '../lib/errors';
import { usePageTitle } from '../hooks/usePageTitle';
import {
  AlertTriangleIcon,
  CalendarIcon,
  EyeIcon,
  EyeOffIcon,
  InboxIcon,
  SendIcon,
} from '../components/icons';
import { Alert, FullPageLoader, Spinner } from '../components/Feedback';

const schema = z.object({
  email: z
    .string()
    .min(1, 'Saisissez votre adresse email professionnelle.')
    .email('Cette adresse email n’est pas valide.'),
  password: z.string().min(1, 'Saisissez votre mot de passe.'),
});

type FormValues = z.infer<typeof schema>;

interface LocationState {
  from?: string;
}

export function LoginPage() {
  usePageTitle('Connexion');
  const { status, login, sessionNotice, clearSessionNotice } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [showPassword, setShowPassword] = useState(false);

  const state = location.state as LocationState | null;
  const from =
    state?.from && state.from.startsWith('/') && !state.from.startsWith('//')
      ? state.from
      : '/demandes/nouvelle';

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  });

  if (status === 'loading') {
    return <FullPageLoader label="Vérification de votre session…" />;
  }

  if (status === 'authenticated') {
    return <Navigate to={from} replace />;
  }

  const onSubmit = async (values: FormValues) => {
    clearSessionNotice();

    try {
      await login(values.email.trim(), values.password);
      navigate(from, { replace: true });
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setError('root.server', {
          type: 'server',
          message:
            'Email ou mot de passe incorrect. Vérifiez vos identifiants puis réessayez.',
        });
        return;
      }

      setError('root.server', {
        type: 'server',
        message: describeError(error).message,
      });
    }
  };

  return (
    <div className="login-page">
      <main id="main-content" className="login-main" tabIndex={-1}>
        <section className="login-panel" aria-labelledby="login-title">
          <div className="login-brand">
            <span className="brand-mark" aria-hidden="true">
              TL
            </span>
            <span className="brand-text">
              <span className="brand-name">TeamLeave</span>
              <span className="brand-tagline">Gestion des congés</span>
            </span>
          </div>

          <div className="login-panel-content">
            <p className="eyebrow">Espace collaborateur</p>
            <h1 id="login-title" className="login-title">
              Bienvenue sur TeamLeave
            </h1>
            <p className="login-lead">
              Connectez-vous pour soumettre vos demandes, consulter les absences
              de votre équipe et, si vous êtes manager, traiter les demandes de
              vos collaborateur·rices.
            </p>

            {sessionNotice ? (
              <Alert tone="info" title="Information">
                {sessionNotice}
              </Alert>
            ) : null}

            <form className="login-form" onSubmit={handleSubmit(onSubmit)} noValidate>
              <div className="field">
                <label htmlFor="email">Adresse email professionnelle</label>
                <input
                  id="email"
                  type="email"
                  className="input"
                  autoComplete="username"
                  inputMode="email"
                  spellCheck={false}
                  aria-invalid={Boolean(errors.email)}
                  aria-describedby={errors.email ? 'email-error' : undefined}
                  {...register('email')}
                />
                {errors.email ? (
                  <p className="field-error" id="email-error">
                    {errors.email.message}
                  </p>
                ) : null}
              </div>

              <div className="field">
                <label htmlFor="password">Mot de passe</label>
                <div className="input-with-action">
                  <input
                    id="password"
                    type={showPassword ? 'text' : 'password'}
                    className="input"
                    autoComplete="current-password"
                    aria-invalid={Boolean(errors.password)}
                    aria-describedby={errors.password ? 'password-error' : undefined}
                    {...register('password')}
                  />
                  <button
                    type="button"
                    className="input-action"
                    onClick={() => setShowPassword((value) => !value)}
                    aria-label={
                      showPassword
                        ? 'Masquer le mot de passe'
                        : 'Afficher le mot de passe'
                    }
                    aria-pressed={showPassword}
                  >
                    {showPassword ? <EyeOffIcon size={18} /> : <EyeIcon size={18} />}
                  </button>
                </div>
                {errors.password ? (
                  <p className="field-error" id="password-error">
                    {errors.password.message}
                  </p>
                ) : null}
              </div>

              {errors.root?.server ? (
                <div className="form-error" role="alert">
                  <AlertTriangleIcon size={18} />
                  <span>{errors.root.server.message}</span>
                </div>
              ) : null}

              <button
                type="submit"
                className="btn btn-primary btn-block btn-lg"
                disabled={isSubmitting}
              >
                {isSubmitting ? <Spinner /> : null}
                {isSubmitting ? 'Connexion…' : 'Se connecter'}
              </button>
            </form>

            <p className="login-help">
              Votre compte est créé par l’Admin RH. En cas d’oubli de mot de
              passe, contactez l’Admin RH : aucune réinitialisation par email
              n’est disponible.
            </p>
          </div>
        </section>

        <aside className="login-aside" aria-labelledby="login-aside-title">
          <div className="login-aside-inner">
            <span className="login-pill">Simple · Sobre · Accessible</span>
            <h2 id="login-aside-title" className="login-aside-title">
              L’essentiel des congés, au même endroit.
            </h2>

            <ul className="login-features">
              <li>
                <span className="login-feature-icon" aria-hidden="true">
                  <SendIcon size={18} />
                </span>
                <div>
                  <strong>Demander</strong>
                  <p>Un formulaire court, un solde visible, une confirmation immédiate.</p>
                </div>
              </li>
              <li>
                <span className="login-feature-icon" aria-hidden="true">
                  <CalendarIcon size={18} />
                </span>
                <div>
                  <strong>Anticiper</strong>
                  <p>Le calendrier d’équipe ne montre que les absences approuvées.</p>
                </div>
              </li>
              <li>
                <span className="login-feature-icon" aria-hidden="true">
                  <InboxIcon size={18} />
                </span>
                <div>
                  <strong>Décider</strong>
                  <p>Les managers voient le solde et l’alerte de capacité avant de statuer.</p>
                </div>
              </li>
            </ul>

            <p className="login-aside-note">
              Chaque décision est tracée. Un refus exige un commentaire.
            </p>
          </div>
        </aside>
      </main>
    </div>
  );
}
