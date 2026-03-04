using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Wololo.Text.Json;

public static class Utf8JsonHelpers
{
    private static object? GetNumber(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt32(out int intValue))
            return intValue;
        if (reader.TryGetInt64(out long longValue))
            return longValue;
        return reader.GetDouble();
    }

    public static object? GetValue(ref Utf8JsonReader reader) =>
        reader.TokenType switch
        {
            JsonTokenType.PropertyName or JsonTokenType.Comment or JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => GetNumber(ref reader),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => null,
        };
}
