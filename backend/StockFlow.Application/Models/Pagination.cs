namespace StockFlow.Application.Models;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class Pagination
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
    private const int MaxPage = 1_000_000;

    public static (int Page, int PageSize, int Skip) Normalize(int page, int pageSize)
    {
        var normalizedPage = Math.Clamp(page, 1, MaxPage);
        var normalizedPageSize = Math.Clamp(
            pageSize <= 0 ? DefaultPageSize : pageSize,
            1,
            MaxPageSize);

        return (normalizedPage, normalizedPageSize, (normalizedPage - 1) * normalizedPageSize);
    }
}
