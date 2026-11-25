// Contrat d'API TeamLeave (step2_arch.md §5), aligné sur backend/src/TeamLeave.Application/Contracts/Dtos.cs.
// Seules les ressources utilisées par les écrans de l'étape 4 sont typées ici.
// Énumérations : chaînes anglaises stables (JsonStringEnumConverter côté API), traduites par l'interface.

export type Guid = string;
/** Date civile `YYYY-MM-DD`, sans heure ni fuseau. */
export type IsoDate = string;
/** Horodatage ISO 8601 en UTC (`...Z`). */
export type IsoDateTime = string;

export type Role = 'Employee' | 'Manager' | 'HrAdmin';
export type LeaveRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface PersonRef {
  id: Guid;
  fullName: string;
}

export interface LeaveTypeRef {
  id: Guid;
  code: string;
  label: string;
}

export interface Rate {
  absent: number;
  teamSize: number;
  /** Arrondi à l'entier, pour l'affichage uniquement (RB-20). */
  percent: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// --- Authentification et profil (§5.2) ---

export interface LoginRequest {
  email: string;
  password: string;
}

export interface TokenResponse {
  accessToken: string;
  tokenType: 'Bearer';
  expiresAtUtc: IsoDateTime;
  mustChangePassword: boolean;
}

export interface Me {
  id: Guid;
  email: string;
  firstName: string;
  lastName: string;
  roles: Role[];
  manager: PersonRef | null;
  mustChangePassword: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface Balance {
  leaveType: LeaveTypeRef;
  amount: number;
  pending: number;
  available: number;
}

// --- Types de congé et jours fériés (§5.6) ---

export interface LeaveType {
  id: Guid;
  code: string;
  label: string;
  isTracked: boolean;
  allowsRetroactive: boolean;
  isSystem: boolean;
  isActive: boolean;
}

export interface Holiday {
  date: IsoDate;
  label: string;
}

// --- Demandes de congé (§5.3) ---

export interface SimulateRequest {
  leaveTypeId: Guid;
  startDate: IsoDate;
  endDate: IsoDate;
}

export interface SubmitLeaveRequest extends SimulateRequest {
  /** 500 caractères au plus. */
  comment?: string | null;
}

export interface SimulationBalance {
  amount: number;
  pending: number;
  availableBefore: number;
  availableAfter: number;
}

/** Règles d'acceptabilité renvoyées en 200 par la simulation (GAP-10). */
export type BlockingReason = 'INSUFFICIENT_BALANCE' | 'PAST_DATE_NOT_ALLOWED' | 'OVERLAPS_OWN_REQUEST';

export interface Simulation {
  daysCount: number;
  workingDays: IsoDate[];
  /** `null` pour un type non soumis à décompte. */
  balance: SimulationBalance | null;
  isSubmittable: boolean;
  blockingReasons: BlockingReason[];
}

export interface HistoryEntry {
  fromStatus: LeaveRequestStatus | null;
  toStatus: LeaveRequestStatus;
  /** `null` pour une action du système. */
  author: PersonRef | null;
  occurredAtUtc: IsoDateTime;
  comment: string | null;
}

export interface OverlapAcknowledgement {
  acknowledgedBy: PersonRef;
  acknowledgedAtUtc: IsoDateTime;
  rate: Rate;
}

export interface Decision {
  outcome: LeaveRequestStatus;
  decidedBy: PersonRef;
  decidedAtUtc: IsoDateTime;
  comment: string | null;
  overlapAcknowledgement: OverlapAcknowledgement | null;
}

export interface Cancellation {
  cancelledBy: PersonRef | null;
  isSystem: boolean;
  cancelledAtUtc: IsoDateTime;
  reason: string | null;
}

export interface OverlapDay {
  date: IsoDate;
  rate: Rate;
  absentColleagues: PersonRef[];
}

export interface OverlapAnalysis {
  leaveRequestId: Guid;
  computedAtUtc: IsoDateTime;
  /** `false` si le demandeur n'a pas de manager ou si l'équipe est vide (RB-16, EX-4). */
  applicable: boolean;
  teamSize: number;
  isAlert: boolean;
  maxRate: Rate | null;
  daysOverThreshold: OverlapDay[];
}

export interface LeaveRequest {
  id: Guid;
  requester: PersonRef;
  leaveType: LeaveTypeRef;
  startDate: IsoDate;
  endDate: IsoDate;
  daysCount: number;
  comment: string | null;
  status: LeaveRequestStatus;
  submittedAtUtc: IsoDateTime;
  decision: Decision | null;
  cancellation: Cancellation | null;
  history: HistoryEntry[];
  /** Renseigné seulement pour l'approbateur / l'admin RH d'une demande en attente (GAP-24). */
  overlap: OverlapAnalysis | null;
}

export interface OverlapAlert {
  isAlert: boolean;
  maxRatePercent: number;
}

export interface LeaveRequestSummary {
  id: Guid;
  requester: PersonRef;
  leaveType: LeaveTypeRef;
  startDate: IsoDate;
  endDate: IsoDate;
  daysCount: number;
  status: LeaveRequestStatus;
  submittedAtUtc: IsoDateTime;
  overlapAlert: OverlapAlert | null;
}

export interface ApproveRequest {
  /** 1 000 caractères au plus. */
  comment?: string | null;
  overlapAcknowledged: boolean;
}

export interface RejectRequest {
  /** Obligatoire, non vide (RB-15). 1 000 caractères au plus. */
  comment: string;
}

// --- Calendrier (§5.5) ---

export type CalendarScope = 'team' | 'reports' | 'all';

export interface CalendarAbsence {
  user: PersonRef;
  /** Dates de la demande, non tronquées à la fenêtre demandée. */
  startDate: IsoDate;
  endDate: IsoDate;
  /** `null` en portée `team` (affiché « Absent »). */
  leaveType: LeaveTypeRef | null;
}

export interface Calendar {
  from: IsoDate;
  to: IsoDate;
  scope: CalendarScope;
  absences: CalendarAbsence[];
}

// --- Erreurs (Problem Details RFC 9457, §5.1 et §5.8) ---

export type ErrorCode =
  | 'VALIDATION_ERROR'
  | 'REJECTION_COMMENT_REQUIRED'
  | 'RANGE_TOO_LONG'
  | 'UNAUTHENTICATED'
  | 'INVALID_CREDENTIALS'
  | 'FORBIDDEN'
  | 'SELF_APPROVAL_FORBIDDEN'
  | 'NOT_APPROVER'
  | 'NOT_REQUESTER'
  | 'PASSWORD_CHANGE_REQUIRED'
  | 'NOT_FOUND'
  | 'INVALID_STATUS_TRANSITION'
  | 'CONCURRENCY_CONFLICT'
  | 'NO_WORKING_DAY'
  | 'PAST_DATE_NOT_ALLOWED'
  | 'PERIOD_TOO_LONG'
  | 'LEAVE_TYPE_INACTIVE'
  | 'INSUFFICIENT_BALANCE'
  | 'OVERLAPS_OWN_REQUEST'
  | 'OVERLAP_NOT_ACKNOWLEDGED'
  | 'INVALID_CURRENT_PASSWORD'
  | 'ACCOUNT_LOCKED'
  | 'RATE_LIMITED';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string | null;
  /** Code stable (§5.8). Absent sur une erreur 500. */
  code?: ErrorCode | string;
  traceId?: string;
  /** Erreurs de format par champ (camelCase). */
  errors?: Record<string, string[]>;
  /** 429 ACCOUNT_LOCKED. */
  lockoutEndUtc?: IsoDateTime;
  /** 422 OVERLAP_NOT_ACKNOWLEDGED : analyse recalculée. */
  overlap?: OverlapAnalysis;
}
