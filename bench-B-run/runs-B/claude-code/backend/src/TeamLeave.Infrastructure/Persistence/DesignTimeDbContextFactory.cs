using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TeamLeave.Infrastructure.Persistence;

/// <summary>
/// Fabrique utilisée par <c>dotnet ef</c> pour générer les migrations SQL Server.
/// La chaîne n'est jamais utilisée pour se connecter lors de la génération.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TeamLeaveDbContext>
{
    public TeamLeaveDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TeamLeaveDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=TeamLeave;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly(typeof(TeamLeaveDbContext).Assembly.FullName))
            .Options;
        return new TeamLeaveDbContext(options);
    }
}
