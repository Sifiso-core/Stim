using System.Linq.Dynamic.Core;

namespace Stim.Api.Services.Sorting;

public class SortMappingProvider(IEnumerable<ISortMappingDefinition> sortMappingsDefinitions)
{
    public SortMapping[] GetMappings<TSource, TDestination>()
    {
        var sortMappingDefinition = sortMappingsDefinitions.OfType<SortMappingDefinition<TSource, TDestination>>().FirstOrDefault();

        if (sortMappingDefinition is null)
        {
            throw new InvalidOperationException($"The mapping from {typeof(TSource).Name} to {typeof(TDestination).Name} is not defined");
        }
        return sortMappingDefinition.Mappings;
    }
    public bool ValidateMappings<TSource, TDestination>(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return true;
        }

        var sortFields = sort.Split(',').Select(f => f.Trim().Split(' ')[0]).Where(f => !string.IsNullOrWhiteSpace(f)).ToList();

        var sortMapping = GetMappings<TSource, TDestination>();

        return sortFields.All(f => sortMapping.Any(m => m.SortField.Equals(f, StringComparison.OrdinalIgnoreCase)));
    }

}
