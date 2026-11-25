import { ApiError, NETWORK_ERROR } from './client';
import { formatDateTimeParis } from '../lib/dates';

// Messages utilisateur par code stable (step2_arch §5.8). Le `detail` du serveur sert de repli.
const MESSAGES: Record<string, string> = {
  [NETWORK_ERROR]: 'Le serveur est injoignable. Vérifiez votre connexion puis réessayez.',
  VALIDATION_ERROR: 'Certains champs sont invalides. Corrigez-les puis réessayez.',
  REJECTION_COMMENT_REQUIRED: 'Un commentaire est obligatoire pour refuser une demande.',
  RANGE_TOO_LONG: 'La période affichée ne peut pas dépasser 93 jours.',
  UNAUTHENTICATED: 'Votre session a expiré. Reconnectez-vous.',
  INVALID_CREDENTIALS: 'Email ou mot de passe incorrect.',
  FORBIDDEN: "Vous n'avez pas les droits nécessaires pour cette action.",
  SELF_APPROVAL_FORBIDDEN: 'Vous ne pouvez pas décider de votre propre demande.',
  NOT_APPROVER: "Vous n'êtes pas l'approbateur de cette demande (le rattachement de l'employé a peut-être changé).",
  PASSWORD_CHANGE_REQUIRED: 'Vous devez changer votre mot de passe avant de continuer.',
  NOT_FOUND: "Cet élément n'existe pas ou ne vous est plus accessible.",
  INVALID_STATUS_TRANSITION: 'Cette demande a déjà été traitée entre-temps.',
  CONCURRENCY_CONFLICT: 'Une modification simultanée a eu lieu. Réessayez.',
  NO_WORKING_DAY: 'La période ne contient aucun jour ouvré (week-ends et jours fériés exclus).',
  PAST_DATE_NOT_ALLOWED: "Ce type de congé n'autorise pas une date de début passée.",
  PERIOD_TOO_LONG: 'La période ne peut pas dépasser 366 jours.',
  LEAVE_TYPE_INACTIVE: "Ce type de congé n'est plus disponible.",
  INSUFFICIENT_BALANCE: 'Votre solde disponible est insuffisant pour cette demande.',
  OVERLAPS_OWN_REQUEST: 'Cette période chevauche une de vos demandes en attente ou approuvées.',
  OVERLAP_NOT_ACKNOWLEDGED: "Plus de la moitié de l'équipe est absente sur au moins un jour : confirmez l'approbation en connaissance de cause.",
  INVALID_CURRENT_PASSWORD: 'Le mot de passe actuel est incorrect.',
  RATE_LIMITED: 'Trop de tentatives de connexion depuis ce poste. Patientez une minute puis réessayez.',
  SERVER_ERROR: 'Une erreur inattendue est survenue côté serveur. Réessayez dans quelques instants.',
};

/** Message lisible pour une erreur quelconque (API, réseau, inattendue). */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.code === 'ACCOUNT_LOCKED') {
      const end = error.problem?.lockoutEndUtc;
      return end
        ? `Compte temporairement verrouillé après plusieurs échecs. Réessayez après ${formatDateTimeParis(end, { timeOnly: true })} (heure de Paris).`
        : 'Compte temporairement verrouillé après plusieurs échecs. Réessayez dans 15 minutes.';
    }
    // Les erreurs métier portent un `detail` précis (ex. « le disponible CP est de 2 jours ») : on le préfère.
    if (error.status === 422 && error.problem?.detail) {
      return error.problem.detail;
    }
    return MESSAGES[error.code] ?? error.problem?.detail ?? error.message;
  }
  return 'Une erreur inattendue est survenue.';
}

/** Erreurs de format par champ, clés normalisées en camelCase sans préfixe `$.`. */
export function fieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError)) {
    return {};
  }
  const result: Record<string, string> = {};
  for (const [rawKey, messages] of Object.entries(error.fieldErrors)) {
    const key = rawKey.replace(/^\$\./, '');
    const normalized = key.charAt(0).toLowerCase() + key.slice(1);
    if (messages[0]) {
      result[normalized] = messages[0];
    }
  }
  return result;
}
