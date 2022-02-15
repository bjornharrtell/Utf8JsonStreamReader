using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Wololo.Text.Json
{
    internal static class Utf8JsonHelpers
    {
        private static string GetString(Utf8JsonReader reader)
        {
            return Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
        }

        private static object GetDecimal(string str)
        {
            var value = double.Parse(str);
            // TODO: check if value can be losslessly converted to float?
            return value;
        }

        private static object GetInteger(string str)
        {
            var value = long.Parse(str);
            if (short.MinValue < value && value < short.MaxValue)
                return (short) value;
            if (int.MinValue < value && value < int.MaxValue)
                return (int) value;
            return value;
        }

        private static object GetNumber(Utf8JsonReader reader)
        {
            var str = GetString(reader);
            if (str.Contains('.'))
                return GetDecimal(str);
            else
                return GetInteger(str);
        }

        public static object? GetValue(Utf8JsonReader reader)
        {
            return reader.TokenType switch
            {
                JsonTokenType.PropertyName or JsonTokenType.Comment or JsonTokenType.String => GetString(reader),
                JsonTokenType.Number => GetNumber(reader),
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                _ => null,
            };
        }
    }
}