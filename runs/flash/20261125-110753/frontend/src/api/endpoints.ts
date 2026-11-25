import { apiRequest } from './client';
import type {
  AuthUser,
  CalendarEntry,
  CreateLeaveRequestInput,
  DecisionInput,
  Holiday,
  LeaveBalance,
  LeaveRequest,
  LeaveType,
  LoginResponse,
} from './types';

export const authApi = {
  login(email: string, password: string): Promise<LoginResponse> {
    return apiRequest<LoginResponse>('/auth/login', {
      method: 'POST',
      body: { email, password },
    });
  },
};

export const meApi = {
  getProfile(): Promise<AuthUser> {
    return apiRequest<AuthUser>('/me');
  },
  getBalances(year: number): Promise<LeaveBalance[]> {
    return apiRequest<LeaveBalance[]>('/me/balances', { query: { year } });
  },
  getLeaveRequests(params: { status?: string; from?: string; to?: string } = {}): Promise<LeaveRequest[]> {
    return apiRequest<LeaveRequest[]>('/me/leave-requests', { query: params });
  },
};

export const leaveRequestsApi = {
  create(input: CreateLeaveRequestInput): Promise<LeaveRequest> {
    return apiRequest<LeaveRequest>('/leave-requests', { method: 'POST', body: input });
  },
  get(id: string): Promise<LeaveRequest> {
    return apiRequest<LeaveRequest>(`/leave-requests/${id}`);
  },
  decide(id: string, input: DecisionInput): Promise<LeaveRequest> {
    return apiRequest<LeaveRequest>(`/leave-requests/${id}/decision`, {
      method: 'POST',
      body: input,
    });
  },
};

export const teamsApi = {
  getPendingRequests(teamId: string): Promise<LeaveRequest[]> {
    return apiRequest<LeaveRequest[]>(`/teams/${teamId}/leave-requests`, {
      query: { status: 'PENDING' },
    });
  },
  getCalendar(teamId: string, from: string, to: string): Promise<CalendarEntry[]> {
    return apiRequest<CalendarEntry[]>(`/teams/${teamId}/calendar`, {
      query: { from, to },
    });
  },
};

export const leaveTypesApi = {
  list(): Promise<LeaveType[]> {
    return apiRequest<LeaveType[]>('/leave-types');
  },
};

export const holidaysApi = {
  list(year: number): Promise<Holiday[]> {
    return apiRequest<Holiday[]>('/holidays', { query: { year } });
  },
};
