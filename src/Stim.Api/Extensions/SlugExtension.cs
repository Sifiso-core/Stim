using System;
using Slugify;

namespace Stim.Api.Extensions;

public static class SlugExtension
{
    private static readonly SlugHelper slugHelper = new();
    public static string ToSlug(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }
        return slugHelper.GenerateSlug(input);
    }
}
