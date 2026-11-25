using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using TeamLeave.Application.Abstractions;
using TeamLeave.Infrastructure.Email;

namespace TeamLeave.IntegrationTests.Support;

/// <summary>
/// Application complète en mémoire (step2_arch §8.2) : base SQLite dans un fichier temporaire par test,
/// horloge fixée au lundi 2026-03-02 09:00 Europe/Paris, expéditeur SMTP factice, aucun service de fond.
/// </summary>
public sealed class TeamLeaveFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset StartUtc = new(2026, 3, 2, 8, 0, 0, TimeSpan.Zero);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"teamleave-tests-{Guid.NewGuid():N}.db");

    public FakeTimeProvider Clock { get; } = new(StartUtc);

    public FakeEmailSender Emails { get; } = new();

    public InterleavingInterceptor Interleaver { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TeamLeave", $"Data Source={_databasePath};Pooling=False");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:AutoMigrate", "true");
        builder.UseSetting("Seed:Holidays", "false");
        builder.UseSetting("Notifications:DispatcherEnabled", "false");
        builder.UseSetting("PasswordHashing:Iterations", "1000");
        builder.UseSetting("RateLimiting:LoginPermitLimit", "1000");
        builder.UseSetting("Jwt:SigningKeys:Current", Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            var dispatcher = services.FirstOrDefault(d => d.ImplementationType == typeof(OutboxDispatcher));
            if (dispatcher is not null)
            {
                services.Remove(dispatcher);
            }

            services.AddSingleton<IInterceptor>(Interleaver);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // Fichier temporaire : sans conséquence.
        }
    }
}

/// <summary>Expéditeur factice : capture les messages ; peut simuler une panne SMTP (CA-13.4).</summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly List<EmailMessage> _sent = [];

    public bool Fail { get; set; }

    public IReadOnlyList<EmailMessage> Sent
    {
        get
        {
            lock (_sent)
            {
                return _sent.ToList();
            }
        }
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (Fail)
        {
            throw new InvalidOperationException("SMTP 451 4.7.0 Temporary server error");
        }

        lock (_sent)
        {
            _sent.Add(message);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Entrelacement déterministe (step2_arch §8.2) : exécute une action juste avant le prochain SaveChanges,
/// ce qui reproduit « lecture T1, lecture T2, validation T2, validation T1 ».
/// </summary>
public sealed class InterleavingInterceptor : SaveChangesInterceptor
{
    private Func<Task>? _beforeNextSave;

    public void BeforeNextSave(Func<Task> action) => _beforeNextSave = action;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var action = Interlocked.Exchange(ref _beforeNextSave, null);
        if (action is not null)
        {
            await action();
        }

        return result;
    }
}
