using System.Text.Json;

namespace Wololo.Text.Json;

public sealed partial class Utf8JsonStreamReader
{
    public readonly record struct JsonResult(JsonTokenType TokenType = JsonTokenType.None, object? Value = null);
}