using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeamLeave.Api.Bootstrap;
using TeamLeave.Api.Endpoints;
using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure;
using TeamLeave.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Journalisation console structurée : évite la dépendance au journal d'événements Windows
// en conteneur et permet à App Service de collecter stdout/stderr.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.UseUtcTimestamp = true;
});

// En production, la clé vient d'un coffre de secrets. En développement uniquement,
// une clé éphémère est générée pour permettre un démarrage immédiat.
var signingKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    if (builder.Environment.IsDevelopment())
    {
        signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        builder.Configuration["Jwt:SigningKey"] = signingKey;
        Console.WriteLine("Jwt:SigningKey absent : clé éphémère générée pour le développement local.");
    }
    else
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey est obligatoire hors développement. Fournissez-le via Key Vault ou une variable d'environnement.");
    }
}

builder.Services.AddTeamLeaveInfrastructure(builder.Configuration);

if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("TeamLeave")))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:TeamLeave est obligatoire hors développement.");
    }

    var emailEnabled = builder.Configuration.GetValue<bool>("Email:Enabled");
    var emailHost = builder.Configuration["Email:Host"];
    if (!emailEnabled || string.IsNullOrWhiteSpace(emailHost))
    {
        throw new InvalidOperationException(
            "Email:Enabled et Email:Host sont obligatoires hors développement.");
    }
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://api.teamleave.example",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "teamleave-api",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<TeamLeaveDbContext>();
                var subject = context.Principal?.FindFirstValue("sub");
                var stamp = context.Principal?.FindFirstValue("sv");

                if (!Guid.TryParse(subject, out var userId) || !Guid.TryParse(stamp, out var securityStamp))
                {
                    context.Fail("Jeton invalide.");
                    return;
                }

                var user = await db.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.Id == userId, context.HttpContext.RequestAborted);

                if (user is null || !user.IsActive || user.SecurityStamp != securityStamp)
                {
                    context.Fail("Jeton révoqué.");
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await ApiProblem.WriteAsync(context.HttpContext, ApiProblem.Unauthenticated());
            },
            OnForbidden = async context =>
            {
                await ApiProblem.WriteAsync(context.HttpContext, ApiProblem.Forbidden());
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("ManagerOnly", policy => policy.RequireRole(RoleNames.MANAGER));
    options.AddPolicy("HrAdminOnly", policy => policy.RequireRole(RoleNames.ADMIN_RH));
});

var allowedOrigins = builder.Configuration.GetSection("Spa:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Spa", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.AddPolicy("refresh", context =>
    {
        var key = context.User.FindFirstValue("sub")
                  ?? context.Connection.RemoteIpAddress?.ToString()
                  ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "900";
        await ApiProblem.WriteAsync(
            context.HttpContext,
            AppError.Create(
                ErrorCodes.RATE_LIMITED,
                "Trop de requêtes",
                429,
                "La limite de tentatives est atteinte. Réessayez plus tard."));
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // App Service termine TLS en amont ; les adresses du proxy sont dynamiques.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (args.Contains("--create-admin", StringComparer.OrdinalIgnoreCase))
{
    Environment.ExitCode = await AdminBootstrapper.RunAsync(app.Services);
    return;
}

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("Spa");
app.UseMiddleware<OriginGuardMiddleware>();
app.UseRateLimiter();

app.UseStatusCodePages(async statusContext =>
{
    if (statusContext.HttpContext.Response.StatusCode == StatusCodes.Status405MethodNotAllowed)
    {
        await ApiProblem.WriteAsync(
            statusContext.HttpContext,
            AppError.Create(
                ErrorCodes.METHOD_NOT_ALLOWED,
                "Méthode non autorisée",
                405,
                "Cette ressource ne prend pas en charge la méthode HTTP demandée."));
    }
});

app.UseAuthentication();
app.UseMiddleware<PasswordChangeGateMiddleware>();
app.UseAuthorization();

app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapManagerEndpoints();
app.MapCalendarEndpoints();
app.MapLeaveTypeEndpoints();
app.MapAdminEndpoints();

app.Run();

public partial class Program;
