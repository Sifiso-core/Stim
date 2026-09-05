using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Stim.Api.Models.Cursor;

public class CursorHelper
{
    private CursorHelper() { }
    public static string Encode(Cursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        var jsonBytes = JsonSerializer.Serialize(cursor);

        return Base64UrlEncoder.Encode(jsonBytes);
    }
    public static Cursor? Decode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var jsonBytes = Base64UrlEncoder.Decode(token);

            return JsonSerializer.Deserialize<Cursor>(jsonBytes);
        }
        catch (Exception)
        {
            return null;
        }
    }
}