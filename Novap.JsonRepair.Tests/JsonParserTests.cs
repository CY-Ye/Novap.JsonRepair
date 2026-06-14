using Novap.JsonRepair.Parsing;

namespace Novap.JsonRepair.Tests;

public class JsonParserTests
{
    [Fact]
    public void ParseJson_EmptyInput_ReturnsNull()
    {
        var result = JsonParser.Parse("");
        Assert.Null(result);
    }

    [Fact]
    public void ParseJson_WhitespaceOnly_ReturnsNull()
    {
        var result = JsonParser.Parse("   ");
        Assert.Null(result);
    }

    [Fact]
    public void ParseJson_SimpleObject_ParsesCorrectly()
    {
        var result = JsonParser.Parse("{\"name\":\"Alice\",\"age\":30}");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Alice", obj["name"]);
        Assert.Equal(30L, obj["age"]);
    }

    [Fact]
    public void ParseJson_SimpleArray_ParsesCorrectly()
    {
        var result = JsonParser.Parse("[1,2,3]");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal(3, arr.Count);
        Assert.Equal(1L, arr[0]);
    }

    [Fact]
    public void ParseJson_NestedStructure_ParsesCorrectly()
    {
        var json = """{"users":[{"name":"Bob"}]}""";
        var result = JsonParser.Parse(json);
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        var users = Assert.IsType<List<object?>>(obj["users"]);
        var user = Assert.IsType<Dictionary<string, object?>>(users[0]);
        Assert.Equal("Bob", user["name"]);
    }

    // --- Number Tests ---
    [Theory]
    [InlineData("42", 42L)]
    [InlineData("-7", -7L)]
    [InlineData("3.14", 3.14)]
    [InlineData("1e10", 1e10)]
    public void ParseNumber_VariousFormats(string input, object expected)
    {
        var result = JsonParser.Parse($"[{input}]");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal(expected, arr[0]);
    }

    // --- String Tests ---
    [Fact]
    public void ParseString_DoubleQuoted()
    {
        var result = JsonParser.Parse("""["hello"]""");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal("hello", arr[0]);
    }

    [Fact]
    public void ParseString_SingleQuotes()
    {
        var result = JsonParser.Parse("['hello']");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal("hello", arr[0]);
    }

    [Fact]
    public void ParseString_BooleanLiterals()
    {
        var result = JsonParser.Parse("[true, false, null]");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal(true, arr[0]);
        Assert.Equal(false, arr[1]);
        Assert.Null(arr[2]);
    }

    [Fact]
    public void ParseString_MissingQuotes_InArray()
    {
        var result = JsonParser.Parse("[hello world]");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal("hello world", arr[0]);
    }

    // --- Array Tests ---
    [Fact]
    public void ParseArray_Empty()
    {
        var result = JsonParser.Parse("[]");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Empty(arr);
    }

    [Fact]
    public void ParseArray_TrailingComma()
    {
        var result = JsonParser.Parse("[1,2,3,]");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal(3, arr.Count);
    }

    [Fact]
    public void ParseArray_MissingClosingBracket()
    {
        var result = JsonParser.Parse("[1,2,3");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal(3, arr.Count);
    }

    [Fact]
    public void ParseArray_MixedTypes()
    {
        var result = JsonParser.Parse("""[1, "two", true, null]""");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal(4, arr.Count);
        Assert.Equal(1L, arr[0]);
        Assert.Equal("two", arr[1]);
        Assert.Equal(true, arr[2]);
        Assert.Null(arr[3]);
    }

    // --- Object Tests ---
    [Fact]
    public void ParseObject_Simple()
    {
        var result = JsonParser.Parse("""{"a":1,"b":"two"}""");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(1L, obj["a"]);
        Assert.Equal("two", obj["b"]);
    }

    [Fact]
    public void ParseObject_TrailingComma()
    {
        var result = JsonParser.Parse("""{"a":1,}""");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Single(obj);
    }

    [Fact]
    public void ParseObject_MissingClosingBrace()
    {
        var result = JsonParser.Parse("""{"a":1,"b":2""");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(2, obj.Count);
    }

    [Fact]
    public void ParseObject_UnquotedKeys()
    {
        var result = JsonParser.Parse("{name:\"Alice\",age:30}");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Alice", obj["name"]);
        Assert.Equal(30L, obj["age"]);
    }

    [Fact]
    public void ParseObject_SingleQuotes()
    {
        var result = JsonParser.Parse("{'key':'value'}");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("value", obj["key"]);
    }

    [Fact]
    public void ParseObject_WithComments()
    {
        var result = JsonParser.Parse("""
            {
                "a": 1,
                "b": 2
            }
            """);
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(2, obj.Count);
    }

    // --- Comment Tests ---
    [Fact]
    public void ParseComment_SingleLine()
    {
        var result = JsonParser.Parse("// comment\n{\"a\": 1}");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(1L, obj["a"]);
    }

    [Fact]
    public void ParseComment_Hash()
    {
        var result = JsonParser.Parse("# comment\n{\"a\": 1}");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(1L, obj["a"]);
    }

    [Fact]
    public void ParseComment_BlockComment()
    {
        var result = JsonParser.Parse("/* block */\n{\"a\": 1}");
        var obj = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(1L, obj["a"]);
    }

    [Fact]
    public void ParseComment_InsideArray_Ignored()
    {
        var result = JsonParser.Parse("[1, // comment\n2]");
        var arr = Assert.IsType<List<object?>>(result);
        Assert.Equal(2, arr.Count);
        Assert.Equal(1L, arr[0]);
        Assert.Equal(2L, arr[1]);
    }

    [Fact]
    public void ParseJson_NoJsonContent_ReturnsNull()
    {
        var result = JsonParser.Parse("just plain text");
        Assert.Null(result);
    }
}
