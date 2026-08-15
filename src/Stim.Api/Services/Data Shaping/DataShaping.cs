using System;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;

namespace Stim.Api.Services.Data_Shaping;

public class DataShapingService
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();
    public List<ExpandoObject> ShapeCollectionData<T>(IEnumerable<T> entities, string? fields)
    {
        var fieldHashSet = fields?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(f => f.Trim())
                                                .ToHashSet() ?? [];

        var propertyInfos = PropertiesCache.GetOrAdd(typeof(T), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        if (fieldHashSet.Any())
        {
            propertyInfos = propertyInfos.Where(p => fieldHashSet.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
        }

        var shapedObjects = new List<ExpandoObject>();

        foreach (var entity in entities)
        {
            IDictionary<string, object?> shapedObject = new ExpandoObject();
            foreach (var propertyInfo in propertyInfos)
            {
                shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
            }
            shapedObjects.Add((ExpandoObject)shapedObject);
        }
        return shapedObjects;
    }
    public ExpandoObject ShapeData<T>(T entity, string? fields)
    {
        var fieldHashSet = fields?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(f => f.Trim())
                                                .ToHashSet() ?? [];

        var propertyInfos = PropertiesCache.GetOrAdd(typeof(T), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        if (fieldHashSet.Any())
        {
            propertyInfos = propertyInfos.Where(p => fieldHashSet.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
        }

        IDictionary<string, object?> shapedObject = new ExpandoObject();

        foreach (var propertyInfo in propertyInfos)
        {
            shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
        }

        return (ExpandoObject)shapedObject;
    }
    public bool Validate<T>(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return true;
        }

        var fieldHashSet = fields?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToHashSet() ?? [];

        var propertyInfos = PropertiesCache.GetOrAdd(typeof(T), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        return fieldHashSet.All(f => propertyInfos.Any(p => p.Name.Equals(f, StringComparison.OrdinalIgnoreCase)));

    }
}
