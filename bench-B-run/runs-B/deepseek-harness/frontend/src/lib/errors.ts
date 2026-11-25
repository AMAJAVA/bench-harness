import { ApiError } from '../api/client';
import type { BalanceShortage } from '../api/types';

export interface ErrorDescription {
  title: string;
  message: string;
  code?: string;
}

const CODE_MESSAGES: Record<string, string> = {
  INVALID_CREDENTIALS: 'Identifiants invalides. Vérifiez votre email et votre mot de passe.',
  INVALID_REFRESH_TOKEN: 'Votre session a expiré. Reconnectez-vous.',
  TOKEN_REUSED: 'Votre session a expiré pour raison de sécurité. Reconnectez-vous.',
  UNAUTHENTICATED: 'Votre session a expiré. Reconnectez-vous.',
  PASSWORD_CHANGE_REQUIRED:
    'Vous devez définir un nouveau mot de passe avant de continuer.',
  PASSWORD_POLICY:
    'Le mot de passe ne respecte pas la politique de sécurité (12 caractères minimum).',
  FORBIDDEN: "Votre rôle ne vous autorise pas à effectuer cette action.",
  NOT_FOUND: "Cette ressource est introuvable ou ne fait pas partie de votre périmètre.",
  VALIDATION_ERROR: 'Certains champs sont invalides. Corrigez-les puis réessayez.',
  INVALID_DATES:
    'Les dates sont invalides : choisissez des jours ouvrés, une date de début aujourd’hui ou future, et une fin postérieure au début.',
  INVALID_HALF_DAY: 'Les demi-journées doivent être « matin » ou « après-midi ».',
  INACTIVE_LEAVE_TYPE: "Ce type de congé n'est plus actif.",
  INSUFFICIENT_BALANCE:
    'Votre solde disponible est insuffisant pour au moins une des années concernées.',
  INSUFFICIENT_BALANCE_AT_APPROVAL:
    'Le solde du demandeur ne permet plus cette approbation ; aucun jour n’a été débité.',
  COMMENT_REQUIRED: 'Un commentaire est obligatoire pour refuser une demande.',
  PERSONAL_OVERLAP:
    'Une de vos demandes actives couvre déjà une des demi-journées demandées.',
  NO_APPROVER:
    "Aucun manager direct actif n'est désigné pour votre compte. Contactez l'Admin RH.",
  OVERLAP_ACK_REQUIRED:
    'Le signal de chevauchement est actif : cochez l’acquittement explicite avant d’approuver.',
  INVALID_STATE:
    'La demande a changé d’état entre-temps. Rechargez la file pour voir son statut actuel.',
  PERIOD_TOO_LONG: 'La période demandée dépasse la durée maximale d’un an.',
  RATE_LIMITED: 'Trop de tentatives. Réessayez dans quelques minutes.',
  SERVICE_UNAVAILABLE: 'Le service est momentanément indisponible. Réessayez plus tard.',
  INTERNAL_ERROR: 'Une erreur interne est survenue. Réessayez plus tard.',
  NETWORK_ERROR:
    "L'API TeamLeave est injoignable. Vérifiez votre connexion puis réessayez.",
};

export function describeError(error: unknown): ErrorDescription {
  if (error instanceof ApiError) {
    const known = error.code ? CODE_MESSAGES[error.code] : undefined;
    const title =
      error.problem.title ||
      (error.status === 0 ? 'Serveur injoignable' : 'Une erreur est survenue');

    if (error.status === 429 && error.retryAfter) {
      const minutes = Math.max(1, Math.ceil(error.retryAfter / 60));
      return {
        title: 'Trop de tentatives',
        message: `La limite de tentatives est atteinte. Réessayez dans environ ${minutes} minute${minutes > 1 ? 's' : ''}.`,
        code: error.code,
      };
    }

    return {
      title,
      message:
        known ||
        error.problem.detail ||
        error.message ||
        'Une erreur inattendue est survenue.',
      code: error.code,
    };
  }

  if (error instanceof Error) {
    return {
      title: 'Une erreur est survenue',
      message: error.message,
    };
  }

  return {
    title: 'Une erreur est survenue',
    message: 'Une erreur inattendue est survenue. Réessayez.',
  };
}

export function getBalanceShortages(error: unknown): BalanceShortage[] {
  if (error instanceof ApiError && Array.isArray(error.problem.balanceShortages)) {
    return error.problem.balanceShortages;
  }
  return [];
}

export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}

export function fieldErrorsFrom(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.problem.errors) {
    return {};
  }

  const result: Record<string, string> = {};
  for (const [key, messages] of Object.entries(error.problem.errors)) {
    if (Array.isArray(messages) && messages.length > 0) {
      const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
      const message = messages[0];
      if (message) {
        result[camelKey] = message;
      }
    }
  }
  return result;
}
