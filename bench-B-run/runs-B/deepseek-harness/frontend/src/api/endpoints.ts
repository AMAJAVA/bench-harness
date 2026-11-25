import { apiRequest, buildQuery } from './client';
import type {
  ApproveLeaveRequestPayload,
  BalancesResponse,
  CalendarResponse,
  ChangePasswordPayload,
  LeaveRequestDetail,
  LeaveRequestListItem,
  LeaveTypeDto,
  LoginResponse,
  MeResponse,
  PagedResult,
  RejectLeaveRequestPayload,
  SubmitLeaveRequestPayload,
} from './types';

export const api = {
  login(email: string, password: string): Promise<LoginResponse> {
    return apiRequest<LoginResponse>('/auth/login', {
      method: 'POST',
      body: { email, password },
      authenticated: false,
      retryOnUnauthorized: false,
    });
  },

  logout(): Promise<void> {
    return apiRequest<void>('/auth/logout', { method: 'POST' });
  },

  getMe(): Promise<MeResponse> {
    return apiRequest<MeResponse>('/me');
  },

  changePassword(payload: ChangePasswordPayload): Promise<void> {
    // Un 401 sur cet endpoint signifie « mot de passe actuel invalide », pas
    // « session expirée » : on ne déclenche pas de refresh ni de rejeu.
    return apiRequest<void>('/me/password', {
      method: 'PUT',
      body: payload,
      retryOnUnauthorized: false,
    });
  },

  getMyBalances(year: number, signal?: AbortSignal): Promise<BalancesResponse> {
    return apiRequest<BalancesResponse>(
      `/me/balances${buildQuery({ year })}`,
      { signal },
    );
  },

  getLeaveTypes(signal?: AbortSignal): Promise<PagedResult<LeaveTypeDto>> {
    return apiRequest<PagedResult<LeaveTypeDto>>(
      `/leave-types${buildQuery({ page: 1, pageSize: 100 })}`,
      { signal },
    );
  },

  submitLeaveRequest(
    payload: SubmitLeaveRequestPayload,
  ): Promise<LeaveRequestDetail> {
    return apiRequest<LeaveRequestDetail>('/me/leave-requests', {
      method: 'POST',
      body: payload,
    });
  },

  getTeamCalendar(
    teamId: string,
    from: string,
    to: string,
    signal?: AbortSignal,
  ): Promise<CalendarResponse> {
    return apiRequest<CalendarResponse>(
      `/teams/${encodeURIComponent(teamId)}/calendar${buildQuery({ from, to })}`,
      { signal },
    );
  },

  getManagerQueue(
    page: number,
    pageSize: number,
    signal?: AbortSignal,
  ): Promise<PagedResult<LeaveRequestListItem>> {
    return apiRequest<PagedResult<LeaveRequestListItem>>(
      `/manager/leave-requests${buildQuery({
        status: 'PENDING',
        page,
        pageSize,
      })}`,
      { signal },
    );
  },

  getManagerRequest(
    requestId: string,
    signal?: AbortSignal,
  ): Promise<LeaveRequestDetail> {
    return apiRequest<LeaveRequestDetail>(
      `/manager/leave-requests/${encodeURIComponent(requestId)}`,
      { signal },
    );
  },

  approveLeaveRequest(
    requestId: string,
    payload: ApproveLeaveRequestPayload,
  ): Promise<LeaveRequestDetail> {
    return apiRequest<LeaveRequestDetail>(
      `/manager/leave-requests/${encodeURIComponent(requestId)}/approve`,
      { method: 'POST', body: payload },
    );
  },

  rejectLeaveRequest(
    requestId: string,
    payload: RejectLeaveRequestPayload,
  ): Promise<LeaveRequestDetail> {
    return apiRequest<LeaveRequestDetail>(
      `/manager/leave-requests/${encodeURIComponent(requestId)}/reject`,
      { method: 'POST', body: payload },
    );
  },
};
