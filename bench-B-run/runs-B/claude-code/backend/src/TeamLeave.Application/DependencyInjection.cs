using Microsoft.Extensions.DependencyInjection;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Admin;
using TeamLeave.Application.Auth;
using TeamLeave.Application.Calendar;
using TeamLeave.Application.Common;
using TeamLeave.Application.LeaveRequests;
using TeamLeave.Application.Notifications;

namespace TeamLeave.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTeamLeaveApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<Clock>();
        services.AddScoped<UseCaseExecutor>();
        services.AddScoped<AuditWriter>();
        services.AddScoped<HolidayCalendar>();
        services.AddScoped<OverlapService>();
        services.AddScoped<BalanceService>();
        services.AddScoped<NotificationComposer>();
        services.AddScoped<AuthService>();
        services.AddScoped<LeaveRequestService>();
        services.AddScoped<CalendarService>();
        services.AddScoped<LeaveTypeService>();
        services.AddScoped<UserAdminService>();
        services.AddScoped<HolidayService>();
        services.AddScoped<JournalService>();
        services.AddScoped<OutboxProcessor>();
        return services;
    }
}
