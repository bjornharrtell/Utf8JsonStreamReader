using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Wololo.Text.Json
{
    internal static class Utf8JsonHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetString(ref Utf8JsonReader reader)
        {
#if NETSTANDARD2_0
            return Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray());
#else
            return Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetDecimal(string str)
        {
            var value = double.Parse(str);
            // TODO: check if value can be losslessly converted to float?
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetInteger(string str)
        {
            var value = long.Parse(str);
            if (short.MinValue < value && value < short.MaxValue)
                return (short) value;
            if (int.MinValue < value && value < int.MaxValue)
                return (int) value;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetNumber(ref Utf8JsonReader reader)
        {
            var str = GetString(ref reader);
            if (str.Contains('.'))
                return GetDecimal(str);
            else
                return GetInteger(str);
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
}