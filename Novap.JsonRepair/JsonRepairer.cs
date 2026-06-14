using System.Globalization;
using System.Text;
using System.Text.Json;
using Novap.JsonRepair.Parsing;

namespace Novap.JsonRepair;

/// <summary>
/// 从 LLM 原始文本中提取并修复 JSON 的工具类。
/// </summary>
public static class JsonRepairer
{
    /// <summary>
    /// 从 LLM 原始文本中提取并修复 JSON。
    /// 先去除 markdown 代码块，再用递归下降解析器修复。
    /// </summary>
    /// <param name="text">LLM 原始响应文本</param>
    /// <returns>修复后的 JSON 字符串；如果无法提取则返回 null</returns>
    public static string? Repair(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var extracted = StripMarkdownFences(text.Trim());
        if (string.IsNullOrWhiteSpace(extracted))
            return null;

        return RepairCore(extracted);
    }

    /// <summary>
    /// 安全版本，不抛异常。
    /// </summary>
    public static bool TryRepair(string? text, out string? result)
    {
        try
        {
            result = Repair(text);
            return result is not null;
        }
        catch (JsonRepairException)
        {
            result = null;
            return false;
        }
    }

    private static string? RepairCore(string json)
    {
        // 尝试标准解析（快速路径）
        if (IsValidJson(json))
            return json;

        // 递归下降解析修复
        var repaired = JsonParser.Parse(json);
        if (repaired is null)
            return null;

        // 用 Utf8JsonWriter 写出 JSON 字符串（AOT 兼容，无需反射）
        return SerializeToString(repaired);
    }

    /// <summary>
    /// 将解析器返回的原生对象图序列化为 JSON 字符串。
    /// 使用 Utf8JsonWriter 避免反射，AOT 兼容。
    /// </summary>
    private static string SerializeToString(object value)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteValue(writer, value);
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            case Dictionary<string, object?> obj:
                writer.WriteStartObject();
                foreach (var kvp in obj)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteValue(writer, kvp.Value);
                }
                writer.WriteEndObject();
                break;
            case List<object?> arr:
                writer.WriteStartArray();
                foreach (var item in arr)
                    WriteValue(writer, item);
                writer.WriteEndArray();
                break;
            case System.Collections.IEnumerable enumerable:
                // 回退：其他可迭代类型当作数组处理
                writer.WriteStartArray();
                foreach (var item in enumerable)
                    WriteValue(writer, item);
                writer.WriteEndArray();
                break;
            default:
                // 其他类型转为字符串
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            while (reader.Read()) { }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 去除 markdown 代码块，提取其中内容。
    /// </summary>
    private static string StripMarkdownFences(string text)
    {
        var fenceStart = text.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart < 0)
            return text;

        var contentStart = text.IndexOf('\n', fenceStart);
        if (contentStart < 0)
            return text;
        contentStart++;

        var fenceEnd = text.IndexOf("```", contentStart, StringComparison.Ordinal);
        if (fenceEnd < 0)
            return text;

        return text[contentStart..fenceEnd].Trim();
    }
}
