using System.Text.Json;

namespace Novap.JsonRepair.Tests;

public class JsonRepairerTests
{
    [Fact]
    public void Repair_NullInput_ReturnsNull()
    {
        Assert.Null(JsonRepairer.Repair(null));
    }

    [Fact]
    public void Repair_EmptyInput_ReturnsNull()
    {
        Assert.Null(JsonRepairer.Repair(""));
    }

    [Fact]
    public void Repair_ValidJson_ReturnsSame()
    {
        var input = """{"name":"Alice","age":30}""";
        var result = JsonRepairer.Repair(input);
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("Alice", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void Repair_MarkdownFence_ExtractsContent()
    {
        var input = """
            Here is the JSON:
            ```json
            {"name":"Bob"}
            ```
            """;
        var result = JsonRepairer.Repair(input);
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("Bob", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void Repair_MarkdownFence_NoLanguage()
    {
        var input = """
            ```
            {"key":"value"}
            ```
            """;
        var result = JsonRepairer.Repair(input);
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("value", doc.RootElement.GetProperty("key").GetString());
    }

    [Fact]
    public void Repair_BareJsonWithPreamble()
    {
        var input = "Sure! Here is the JSON:\n{\"name\":\"Charlie\"}\nLet me know.";
        var result = JsonRepairer.Repair(input);
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("Charlie", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void Repair_MalformedJson_SingleQuotesTrailingComma()
    {
        var input = "{'name': 'Dave', 'age': 25,}";
        var result = JsonRepairer.Repair(input);
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("Dave", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(25, doc.RootElement.GetProperty("age").GetInt64());
    }

    [Fact]
    public void Repair_MissingBrackets()
    {
        var input = """{"name":"Eve","items":["a","b"  """;
        var result = JsonRepairer.Repair(input);
        Assert.NotNull(result);
    }

    [Fact]
    public void Repair_NoJsonContent_ReturnsNull()
    {
        Assert.Null(JsonRepairer.Repair("This is just regular text with no JSON."));
    }

    [Fact]
    public void TryRepair_ValidJson_ReturnsTrue()
    {
        Assert.True(JsonRepairer.TryRepair("""{"x":1}""", out var result));
        Assert.NotNull(result);
    }

    [Fact]
    public void TryRepair_InvalidInput_ReturnsFalse()
    {
        Assert.False(JsonRepairer.TryRepair(null, out _));
        Assert.False(JsonRepairer.TryRepair("", out _));
    }

    [Fact]
    public void Repair_Integration_WithJsonSerializer()
    {
        var llmOutput = """
            ```json
            {"characters": [{"name": "Alice", "age": 30}, {"name": "Bob", "age": 25}]}
            ```
            """;
        var json = JsonRepairer.Repair(llmOutput);
        Assert.NotNull(json);
        var doc = JsonDocument.Parse(json);
        var chars = doc.RootElement.GetProperty("characters");
        Assert.Equal(2, chars.GetArrayLength());
        Assert.Equal("Alice", chars[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Repair_MissingBrackets_RoundTripValid()
    {
        var input = """{"name":"Eve","items":["a","b"  """;
        var result = JsonRepairer.Repair(input);
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("Eve", doc.RootElement.GetProperty("name").GetString());
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
    }

    [Fact]
    public void Repair_Malformed_RoundTrip_SerializesCorrectly()
    {
        // 单引号 + 无引号键 + 尾逗号 → 应输出合法 JSON
        var input = "{name: 'Alice', age: 30,}";
        var result = JsonRepairer.Repair(input);
        Assert.NotNull(result);
        // 验证输出是有效 JSON
        var doc = JsonDocument.Parse(result);
        Assert.Equal("Alice", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(30, doc.RootElement.GetProperty("age").GetInt64());
    }
}
