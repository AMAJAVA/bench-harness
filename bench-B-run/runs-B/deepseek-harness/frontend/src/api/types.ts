/**
 * Contrat de l'API TeamLeave v1.
 *
 * Ces types reproduisent les DTO exposés par le backend de l'étape 3
 * (`TeamLeave.Application/Contracts`) et le contrat de l'étape 2 (§5).
 * Les propriétés nullables sont optionnelles : le backend configure
 * `JsonIgnoreCondition.WhenWritingNull`, donc un champ null est absent du JSON.
 */

export type Role = 'EMPLOYE' | 'MANAGER' | 'ADMIN_RH';

export type HalfDay = 'AM' | 'PM';

export type LeaveStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'CANCELLED';

export type DecisionEvent = 'SUBMITTED' | 'APPROVED' | 'REJECTED' | 'CANCELLED';

export interface PersonSummary {
  id: string;
  firstName: string;
  lastName: string;
}

export interface TeamSummary {
  id: string;
  name: string;
}

export interface LeaveTypeSummary {
  id: string;
  code: string;
  label: string;
  color?: string;
}

export interface UserSummary {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: Role[];
  teamId: string;
  mustChangePassword: boolean;
}

export interface MeResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: Role[];
  team?: TeamSummary;
  manager?: PersonSummary;
  mustChangePassword: boolean;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: UserSummary;
}

export interface LeaveTypeDto {
  id: string;
  code: string;
  label: string;
  color: string;
  active: boolean;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface BalanceDto {
  leaveTypeId: string;
  code: string;
  label: string;
  color: string;
  grantedDays: number;
  consumedDays: number;
  availableDays: number;
}

export interface BalancesResponse {
  year: number;
  balances: BalanceDto[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface LeaveRequestListItem {
  id: string;
  leaveType: LeaveTypeSummary;
  startDate: string;
  startHalfDay: HalfDay;
  endDate: string;
  endHalfDay: HalfDay;
  totalDays: number;
  status: LeaveStatus;
  submittedAt: string;
  /** Résumé du signal enregistré à la soumission (file manager uniquement). */
  overlapFlagged?: boolean;
}

export interface Allocation {
  year: number;
  days: number;
}

export interface TimelineEvent {
  event: DecisionEvent;
  at: string;
  by?: PersonSummary;
}

export interface HalfDaySlot {
  date: string;
  halfDay: HalfDay;
}

export interface Acknowledgement {
  by: PersonSummary;
  at: string;
}

export interface Overlap {
  applicable: boolean;
  active: boolean;
  teamId?: string;
  teamSize: number;
  maxRate?: number;
  breachingHalfDays: HalfDaySlot[];
  evaluatedAt: string;
  acknowledgement?: Acknowledgement;
}

export interface EmployeeBalance {
  year: number;
  leaveTypeId: string;
  grantedDays: number;
  consumedDays: number;
  availableDays: number;
}

export interface LeaveRequestDetail {
  id: string;
  employee: PersonSummary;
  leaveType: LeaveTypeSummary;
  startDate: string;
  startHalfDay: HalfDay;
  endDate: string;
  endHalfDay: HalfDay;
  totalDays: number;
  status: LeaveStatus;
  comment?: string;
  submittedAt: string;
  decidedBy?: PersonSummary;
  decidedAt?: string;
  decisionComment?: string;
  cancelledAt?: string;
  cancellationReason?: string;
  allocations: Allocation[];
  timeline: TimelineEvent[];
  overlap?: Overlap;
  employeeBalance?: EmployeeBalance;
  overlapHistory?: Overlap[];
}

export interface CalendarAbsence {
  leaveRequestId?: string;
  employeeId: string;
  employeeDisplayName: string;
  startDate: string;
  startHalfDay: HalfDay;
  endDate: string;
  endHalfDay: HalfDay;
  /** Renseigné si le lecteur est Admin RH, manager direct ou l'employé concerné. */
  leaveType?: LeaveTypeSummary;
}

export interface CalendarResponse {
  teamId: string;
  teamName: string;
  from: string;
  to: string;
  absences: CalendarAbsence[];
}

export interface BalanceShortage {
  leaveTypeId?: string;
  code?: string;
  year: number;
  requestedDays: number;
  availableDays: number;
  missingDays: number;
}

export interface ConflictSlot {
  date: string;
  halfDay: HalfDay;
}

/** Corps `application/problem+json` renvoyé par l'API. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  traceId?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  balanceShortages?: BalanceShortage[];
  conflict?: ConflictSlot;
  overlap?: Overlap;
  [key: string]: unknown;
}

export interface SubmitLeaveRequestPayload {
  leaveTypeId: string;
  startDate: string;
  startHalfDay: HalfDay;
  endDate: string;
  endHalfDay: HalfDay;
  comment?: string;
}

export interface ApproveLeaveRequestPayload {
  comment?: string;
  overlapAcknowledged: boolean;
}

export interface RejectLeaveRequestPayload {
  comment: string;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
}
