namespace Stim.Api.Models.Common;

public class PaginationMetadata
{
    public required PaginationType PaginationType { get; set; }
    public int? Page { get; set; }

    public int? PageSize { get; set; }

    public int? TotalCount { get; set; }

    public int? TotalPages { get; set; }

    public bool HasNextPage { get; set; }

    public bool HasPreviousPage { get; set; }

    public string? NextCursor { get; set; }

    public string? PreviousCursor { get; set; }
}
