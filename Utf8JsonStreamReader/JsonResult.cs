using System.Text.Json;

namespace Wololo.Text.Json;

public readonly record struct JsonResult(JsonTokenType TokenType = JsonTokenType.None, object? Value = null);
