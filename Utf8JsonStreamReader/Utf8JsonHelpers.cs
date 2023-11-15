using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Wololo.Text.Json;

internal static class Utf8JsonHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? GetNumber(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt16(out short shortValue))
            return shortValue;
        if (reader.TryGetInt32(out int intValue))
            return intValue;
        if (reader.TryGetInt64(out long longValue))
            return longValue;
        if (reader.TryGetDouble(out double doubleValue))
            return doubleValue;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GetValue(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.PropertyName or JsonTokenType.Comment or JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => GetNumber(ref reader),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => null,
        };
    }
}