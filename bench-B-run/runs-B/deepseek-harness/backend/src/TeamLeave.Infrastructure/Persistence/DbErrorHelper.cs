using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TeamLeave.Infrastructure.Persistence;

public static class DbErrorHelper
{
    public static bool IsExclusionViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation };

    public static bool IsUniqueViolation(DbUpdateException exception, string? constraint = null)
    {
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            return false;
        }

        return constraint is null || string.Equals(pg.ConstraintName, constraint, StringComparison.Ordinal);
    }

    public static bool IsCheckViolation(DbUpdateException exception, string? constraint = null)
    {
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.CheckViolation } pg)
        {
            return false;
        }

        return constraint is null || string.Equals(pg.ConstraintName, constraint, StringComparison.Ordinal);
    }
}
