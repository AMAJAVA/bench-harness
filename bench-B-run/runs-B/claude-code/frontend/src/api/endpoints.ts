import { apiRequest } from './client';
import type {
  ApproveRequest,
  Balance,
  Calendar,
  CalendarScope,
  ChangePasswordRequest,
  Guid,
  Holiday,
  IsoDate,
  LeaveRequest,
  LeaveRequestSummary,
  LeaveType,
  LoginRequest,
  Me,
  PagedResult,
  RejectRequest,
  SimulateRequest,
  Simulation,
  SubmitLeaveRequest,
  TokenResponse,
} from './types';

// Un appel par couple verbe/chemin du contrat (step2_arch §5), chemins relatifs à /api/v1.

export const api = {
  login: (body: LoginRequest) => apiRequest<TokenResponse>('POST', '/auth/login', { body, anonymous: true }),

  getMe: (signal?: AbortSignal) => apiRequest<Me>('GET', '/me', { signal }),

  changePassword: (body: ChangePasswordRequest) => apiRequest<void>('PUT', '/me/password', { body }),

  getMyBalances: (signal?: AbortSignal) => apiRequest<Balance[]>('GET', '/me/balances', { signal }),

  getLeaveTypes: (signal?: AbortSignal) => apiRequest<LeaveType[]>('GET', '/leave-types', { signal }),

  getHolidays: (year: number, signal?: AbortSignal) => apiRequest<Holiday[]>('GET', '/holidays', { query: { year }, signal }),

  simulate: (body: SimulateRequest, signal?: AbortSignal) =>
    apiRequest<Simulation>('POST', '/leave-requests/simulate', { body, signal }),

  submitLeaveRequest: (body: SubmitLeaveRequest) => apiRequest<LeaveRequest>('POST', '/leave-requests', { body }),

  listToDecide: (page: number, pageSize: number, signal?: AbortSignal) =>
    apiRequest<PagedResult<LeaveRequestSummary>>('GET', '/leave-requests', {
      query: { scope: 'to-decide', page, pageSize },
      signal,
    }),

  getLeaveRequest: (id: Guid, signal?: AbortSignal) =>
    apiRequest<LeaveRequest>('GET', `/leave-requests/${encodeURIComponent(id)}`, { signal }),

  approve: (id: Guid, body: ApproveRequest) =>
    apiRequest<LeaveRequest>('POST', `/leave-requests/${encodeURIComponent(id)}/approve`, { body }),

  reject: (id: Guid, body: RejectRequest) =>
    apiRequest<LeaveRequest>('POST', `/leave-requests/${encodeURIComponent(id)}/reject`, { body }),

  getCalendar: (from: IsoDate, to: IsoDate, scope: CalendarScope, signal?: AbortSignal) =>
    apiRequest<Calendar>('GET', '/calendar', { query: { from, to, scope }, signal }),
};

/** Clés TanStack Query, centralisées pour les invalidations. */
export const queryKeys = {
  me: ['me'] as const,
  balances: ['me', 'balances'] as const,
  leaveTypes: ['leave-types'] as const,
  holidays: (year: number) => ['holidays', year] as const,
  simulation: (input: SimulateRequest) => ['simulation', input.leaveTypeId, input.startDate, input.endDate] as const,
  toDecide: (page: number) => ['leave-requests', 'to-decide', page] as const,
  toDecideAll: ['leave-requests', 'to-decide'] as const,
  leaveRequest: (id: Guid) => ['leave-requests', 'detail', id] as const,
  calendar: (from: IsoDate, to: IsoDate, scope: CalendarScope) => ['calendar', scope, from, to] as const,
};
