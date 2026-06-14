namespace Novap.JsonRepair;

/// <summary>
/// JSON 修复失败时抛出的异常。
/// </summary>
public sealed class JsonRepairException : Exception
{
    /// <summary>尝试修复的原始 JSON。</summary>
    public string? ExtractedJson { get; }

    /// <summary>解析失败的位置。</summary>
    public int Position { get; }

    public JsonRepairException(string message, string? extractedJson = null, int position = -1, Exception? innerException = null)
        : base(message, innerException)
    {
        ExtractedJson = extractedJson;
        Position = position;
    }
}
