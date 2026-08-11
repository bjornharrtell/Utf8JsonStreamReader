using System.Text.Json;

namespace Wololo.Text.Json;

public readonly record struct JsonResult(JsonTokenType TokenType = JsonTokenType.None, object? Value = null)
{
    public bool IsValue =>
        TokenType is JsonTokenType.String or JsonTokenType.Number or JsonTokenType.False or JsonTokenType.True;
}
