using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Entities;
using Stim.Api.Models.Common;
using Stim.Api.Models.Cursor;

namespace Stim.Api.Services.Sorting;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sort, SortMapping[] sortMappings, string defaultOrderBy = "Id")
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return query.OrderBy(defaultOrderBy);
        }
        var sortFields = sort.Split(',')
                                        .Select(s => s.Trim())
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .ToArray();

        var orderByParts = new List<string>();

        foreach (var field in sortFields)
        {
            var (sortField, isDescending) = ParseFields(field);

            var sortMapping = sortMappings.First(m => m.SortField.Equals(sortField, StringComparison.OrdinalIgnoreCase));

            var direction = (isDescending, sortMapping.Reverse) switch
            {
                (true, true) => "ASC",
                (false, true) => "DESC",
                (true, false) => "DESC",
                (false, false) => "ASC"
            };

            orderByParts.Add($"{sortMapping.PropertyName} {direction}");

        }
        var orderBy = string.Join(",", orderByParts);

        return query.OrderBy(orderBy);

    }
    public static async Task<DataCollectionResponse<T>> ToPaginationResultAsync<T>(
    this IQueryable<T> query,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        var hasPreviousPage = page > 1;
        var hasNextPage = page < totalPages;

        return new DataCollectionResponse<T>
        {
            Data = data,

            Pagination = new PaginationMetadata
            {
                PaginationType = PaginationType.Offset,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = hasPreviousPage,
                HasNextPage = hasNextPage
            }
        };
    }
    private static (string SortField, bool IsDescending) ParseFields(string field)
    {
        var parts = field.Split(' ');

        var sortField = parts[0];

        var isDescending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortField, isDescending);
    }


    public static async Task<DataCollectionResponse<T>> ToCursorPaginationResult<T>(
     this IQueryable<T> query,
     string? cursorToken,
     int pageSize,
     CancellationToken cancellationToken = default)
     where T : ICursorPaginatedEntity
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var cursor = CursorHelper.Decode(cursorToken);

        query = ApplyCursorFilterAndOrder(query, cursor);

        var data = await query
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMoreData = data.Count > pageSize;
        TrimAndNormalize(data, cursor, hasMoreData);

        return BuildResponse(data, cursor, pageSize, hasMoreData);
    }

    private static IQueryable<T> ApplyCursorFilterAndOrder<T>(
        IQueryable<T> query,
        Cursor? cursor)
        where T : ICursorPaginatedEntity
    {
        if (cursor is null)
        {
            return query
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id);
        }

        if (cursor.Direction == CursorDirection.Next)
        {
            return query
                .Where(x =>
                    x.CreatedAtUtc < cursor.CreatedAtUtc ||
                    (x.CreatedAtUtc == cursor.CreatedAtUtc &&
                     string.Compare(x.Id, cursor.Id) > 0))
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id);
        }

        return query
            .Where(x =>
                x.CreatedAtUtc > cursor.CreatedAtUtc ||
                (x.CreatedAtUtc == cursor.CreatedAtUtc &&
                 string.Compare(x.Id, cursor.Id) < 0))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id);
    }

    private static void TrimAndNormalize<T>(
        List<T> data,
        Cursor? cursor,
        bool hasMoreData)
        where T : ICursorPaginatedEntity
    {
        if (hasMoreData)
        {
            var indexToRemove = cursor?.Direction == CursorDirection.Previous
                ? 0
                : data.Count - 1;
            data.RemoveAt(indexToRemove);
        }

        if (cursor?.Direction == CursorDirection.Previous)
            data.Reverse();
    }

    private static DataCollectionResponse<T> BuildResponse<T>(
        List<T> data,
        Cursor? cursor,
        int pageSize,
        bool hasMoreData)
        where T : ICursorPaginatedEntity
    {
        var pagination = new PaginationMetadata
        {
            PaginationType = PaginationType.Cursor,
            PageSize = pageSize,
            HasNextPage = false,
            HasPreviousPage = false
        };

        if (data.Count == 0)
        {
            return new DataCollectionResponse<T>
            {
                Data = data,
                Pagination = pagination
            };
        }

        var firstItem = data[0];
        var lastItem = data[^1];

        var hasPreviousPage =
            cursor is not null &&
            (cursor.Direction == CursorDirection.Next || hasMoreData);

        var hasNextPage =
            cursor?.Direction == CursorDirection.Previous || hasMoreData;

        pagination.HasPreviousPage = hasPreviousPage;
        pagination.HasNextPage = hasNextPage;

        if (hasPreviousPage)
        {
            pagination.PreviousCursor = CursorHelper.Encode(
                new Cursor(firstItem.Id, firstItem.CreatedAtUtc, CursorDirection.Previous));
        }

        if (hasNextPage)
        {
            pagination.NextCursor = CursorHelper.Encode(
                new Cursor(lastItem.Id, lastItem.CreatedAtUtc, CursorDirection.Next));
        }

        return new DataCollectionResponse<T>
        {
            Data = data,
            Pagination = pagination
        };
    }


}
