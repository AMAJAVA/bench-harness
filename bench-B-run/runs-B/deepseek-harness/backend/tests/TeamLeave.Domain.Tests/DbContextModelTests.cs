using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Entities;
using TeamLeave.Infrastructure.Persistence;
using Xunit;

namespace TeamLeave.Domain.Tests;

public sealed class DbContextModelTests
{
    [Fact]
    public void Entity_model_can_be_built_without_a_database()
    {
        var options = new DbContextOptionsBuilder<TeamLeaveDbContext>()
            .UseNpgsql("Host=localhost;Database=teamleave_model_test;Username=test;Password=test")
            .Options;

        using var db = new TeamLeaveDbContext(options);

        Assert.NotNull(db.Model.FindEntityType(typeof(User)));
        Assert.NotNull(db.Model.FindEntityType(typeof(LeaveRequest)));
        Assert.NotNull(db.Model.FindEntityType(typeof(AnnualBalance)));
        Assert.NotNull(db.Model.FindEntityType(typeof(BalanceMovement)));
        Assert.NotNull(db.Model.FindEntityType(typeof(OverlapEvaluation)));
        Assert.NotNull(db.Model.FindEntityType(typeof(Notification)));
        Assert.NotNull(db.Model.FindEntityType(typeof(RefreshToken)));
        Assert.NotNull(db.Model.FindEntityType(typeof(AuditEntry)));
    }
}
