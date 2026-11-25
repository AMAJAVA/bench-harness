import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { api } from '../api/endpoints';
import { ApiError } from '../api/client';
import { describeError } from '../lib/errors';
import { useAuth } from '../auth/AuthContext';
import { fullName } from '../lib/dates';
import { usePageTitle } from '../hooks/usePageTitle';
import {
  AlertTriangleIcon,
  LockIcon,
  LogOutIcon,
  ShieldCheckIcon,
} from '../components/icons';
import { Alert, Spinner } from '../components/Feedback';

const schema = z
  .object({
    currentPassword: z.string().min(1, 'Saisissez votre mot de passe actuel.'),
    newPassword: z
      .string()
      .min(12, 'Le nouveau mot de passe doit contenir au moins 12 caractères.')
      .max(128, 'Le nouveau mot de passe ne peut pas dépasser 128 caractères.'),
    confirmPassword: z.string().min(1, 'Confirmez le nouveau mot de passe.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'Les deux mots de passe ne correspondent pas.',
    path: ['confirmPassword'],
  });

type FormValues = z.infer<typeof schema>;

export function PasswordChangeGate() {
  usePageTitle('Nouveau mot de passe');
  const { user, logout, completePasswordChange } = useAuth();
  const [loggingOut, setLoggingOut] = useState(false);
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (values: FormValues) => {
    try {
      await api.changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      completePasswordChange();
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setError('currentPassword', {
          type: 'server',
          message: 'Le mot de passe actuel est invalide.',
        });
        return;
      }

      if (error instanceof ApiError && error.code === 'PASSWORD_POLICY') {
        setError('newPassword', {
          type: 'server',
          message:
            error.problem.detail ??
            'Le mot de passe ne respecte pas la politique de sécurité.',
        });
        return;
      }

      setError('root.server', {
        type: 'server',
        message: describeError(error).message,
      });
    }
  };

  const handleLogout = async () => {
    setLoggingOut(true);
    try {
      await logout();
    } finally {
      setLoggingOut(false);
    }
  };

  return (
    <div className="gate-page">
      <main id="main-content" className="gate-main" tabIndex={-1}>
        <div className="gate-card">
          <div className="gate-brand">
            <span className="brand-mark" aria-hidden="true">
              TL
            </span>
            <span className="brand-name">TeamLeave</span>
          </div>

          <span className="gate-icon" aria-hidden="true">
            <LockIcon size={26} />
          </span>

          <h1 className="gate-title">Définir un nouveau mot de passe</h1>
          <p className="gate-lead">
            Pour des raisons de sécurité, vous devez remplacer le mot de passe
            temporaire avant d’accéder à TeamLeave.
            {user ? ` Compte : ${fullName(user)}.` : ''}
          </p>

          <Alert tone="info" title="Politique de mot de passe">
            12 caractères minimum, 128 maximum. Aucune composition imposée. Le
            changement déconnecte toutes vos sessions.
          </Alert>

          <form className="gate-form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="field">
              <label htmlFor="current-password">Mot de passe actuel</label>
              <input
                id="current-password"
                type="password"
                className="input"
                autoComplete="current-password"
                aria-invalid={Boolean(errors.currentPassword)}
                aria-describedby={
                  errors.currentPassword ? 'current-password-error' : undefined
                }
                {...register('currentPassword')}
              />
              {errors.currentPassword ? (
                <p className="field-error" id="current-password-error">
                  {errors.currentPassword.message}
                </p>
              ) : null}
            </div>

            <div className="field">
              <label htmlFor="new-password">Nouveau mot de passe</label>
              <input
                id="new-password"
                type="password"
                className="input"
                autoComplete="new-password"
                aria-invalid={Boolean(errors.newPassword)}
                aria-describedby={
                  errors.newPassword
                    ? 'new-password-hint new-password-error'
                    : 'new-password-hint'
                }
                {...register('newPassword')}
              />
              <p className="field-hint" id="new-password-hint">
                12 caractères minimum.
              </p>
              {errors.newPassword ? (
                <p className="field-error" id="new-password-error">
                  {errors.newPassword.message}
                </p>
              ) : null}
            </div>

            <div className="field">
              <label htmlFor="confirm-password">Confirmation</label>
              <input
                id="confirm-password"
                type="password"
                className="input"
                autoComplete="new-password"
                aria-invalid={Boolean(errors.confirmPassword)}
                aria-describedby={
                  errors.confirmPassword ? 'confirm-password-error' : undefined
                }
                {...register('confirmPassword')}
              />
              {errors.confirmPassword ? (
                <p className="field-error" id="confirm-password-error">
                  {errors.confirmPassword.message}
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
              className="btn btn-primary btn-block"
              disabled={isSubmitting}
            >
              {isSubmitting ? <Spinner /> : <ShieldCheckIcon size={18} />}
              {isSubmitting
                ? 'Enregistrement…'
                : 'Enregistrer et se reconnecter'}
            </button>
          </form>

          <div className="gate-footer">
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={handleLogout}
              disabled={loggingOut}
            >
              {loggingOut ? <Spinner /> : <LogOutIcon size={18} />}
              {loggingOut ? 'Déconnexion…' : 'Se déconnecter'}
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}
