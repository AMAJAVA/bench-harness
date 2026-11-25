namespace TeamLeave.Application.Common;

public sealed record PageParameters(int Page, int PageSize)
{
    public int Skip => (Page - 1) * PageSize;
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public static PagedResult<T> Empty(int page, int pageSize) => new([], page, pageSize, 0);
}

public static class Pagination
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public static Result<PageParameters> Normalize(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            return Result<PageParameters>.Fail(
                ErrorCodes.INVALID_PAGINATION,
                "Pagination invalide",
                422,
                $"Les paramètres de pagination doivent vérifier page >= 1 et 1 <= pageSize <= {MaxPageSize}.");
        }

        return Result<PageParameters>.Ok(new PageParameters(page, pageSize));
    }
}
