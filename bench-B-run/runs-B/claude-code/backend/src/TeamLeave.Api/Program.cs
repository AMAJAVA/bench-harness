using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using TeamLeave.Api.Infrastructure;
using TeamLeave.Application;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Domain.Users;
using TeamLeave.Infrastructure;
using TeamLeave.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------ Composition (step2_arch §3)
builder.Services.AddTeamLeaveApplication();
builder.Services.AddTeamLeaveInfrastructure(builder.Configuration);
builder.Services.AddSingleton(builder.Configuration.GetSection("App").Get<AppSettings>() ?? new AppSettings());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services
    .AddControllers(options =>
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    })
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ValidationResponses.Create);

builder.Services.AddExceptionHandler<ProblemExceptionHandler>();
builder.Services.AddProblemDetails();

// ------------------------------------------------------------------ Authentification et autorisation (§7.1, §7.2)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.ConfigureOptions<ConfigureJwtBearer>();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.ManagerOrHrAdmin, p => p.RequireRole(nameof(Role.Manager), nameof(Role.HrAdmin)))
    .AddPolicy(Policies.HrAdmin, p => p.RequireRole(nameof(Role.HrAdmin)));

builder.Services.AddLoginRateLimiting();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddOpenApi();

var app = builder.Build();

// ------------------------------------------------------------------ Amorçage des données (§4.4, GAP-16, GAP-17)
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
}

// ------------------------------------------------------------------ Pipeline (§3.4)
app.UseForwardedHeaders();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous();
if (!app.Environment.IsProduction())
{
    app.MapOpenApi().AllowAnonymous();
}

// Repli SPA : index.html pour toute route hors /api et /health (§3.4, étape 10).
if (File.Exists(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html")))
{
    app.MapFallbackToFile("index.html").AllowAnonymous();
}

app.Run();

public partial class Program;
