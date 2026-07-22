using System.Linq.Dynamic.Core;

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
    private static (string SortField, bool IsDescending) ParseFields(string field)
    {
        var parts = field.Split(' ');

        var sortField = parts[0];

        var isDescending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortField, isDescending);
    }
}
