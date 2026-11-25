export type Role = 'EMPLOYE' | 'MANAGER' | 'ADMIN_RH';

export type LeaveStatus = 'PENDING' | 'APPROVED' | 'REJECTED';

export type DecisionAction = 'APPROVE' | 'REJECT';

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: Role[];
  teamId?: string | null;
  team?: { id: string; name: string } | null;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: 'Bearer';
  expiresIn: number;
  user: AuthUser;
}

export interface LeaveBalance {
  leaveTypeId: string;
  leaveTypeCode: string;
  leaveTypeLabel: string;
  year: number;
  acquiredDays: number;
  adjustmentDays: number;
  usedDays: number;
  availableDays: number;
}

export interface LeaveType {
  id: string;
  code: string;
  label: string;
  isActive: boolean;
  trackBalance: boolean;
  displayOrder: number;
}

export interface OverlapDetail {
  date: string;
  absentEmployees: string[];
}

export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeFirstName?: string;
  employeeLastName?: string;
  teamId: string;
  leaveType: { id: string; code: string; label: string };
  startDate: string;
  endDate: string;
  workingDays: number;
  status: LeaveStatus;
  employeeComment?: string | null;
  submittedAt: string;
  deciderId?: string | null;
  decidedAt?: string | null;
  managerComment?: string | null;
  hasOverlapWarning: boolean;
  overlapRatio?: number | null;
  overlapDetails?: OverlapDetail[] | null;
  acknowledgedOverlapBy?: string | null;
  acknowledgedOverlapAt?: string | null;
}

export interface CreateLeaveRequestInput {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  comment?: string;
}

export interface DecisionInput {
  action: DecisionAction;
  comment?: string;
  acknowledgeOverlapWarning: boolean;
}

export interface CalendarEntry {
  leaveRequestId: string;
  employeeId: string;
  employeeFirstName: string;
  employeeLastName: string;
  leaveTypeCode: string;
  leaveTypeLabel: string;
  startDate: string;
  endDate: string;
}

export interface Holiday {
  id: string;
  date: string;
  label: string;
  isActive: boolean;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, string[]>;
}
