namespace Novap.JsonRepair.Parsing;

internal sealed partial class JsonParser
{
    private readonly string _input;
    private int _index;
    private readonly ParseContext _context = new();

    private JsonParser(string json)
    {
        _input = json;
    }

    public static object? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var parser = new JsonParser(json);
        return parser.ParseJson();
    }

    private char? Peek(int offset = 0)
    {
        var pos = _index + offset;
        return pos < _input.Length ? _input[pos] : null;
    }

    private void Advance() => _index++;

    private void SkipWhitespace()
    {
        while (_index < _input.Length && char.IsWhiteSpace(_input[_index]))
            _index++;
    }

    private int ScrollWhitespace(int offset = 0)
    {
        while (_index + offset < _input.Length && char.IsWhiteSpace(_input[_index + offset]))
            offset++;
        return offset;
    }

    private int SkipToChar(char target, int offset = 0)
    {
        int backslashes = 0;
        int i = _index + offset;
        while (i < _input.Length)
        {
            var ch = _input[i];
            if (ch == '\\')
            {
                backslashes++;
                i++;
                continue;
            }
            if (ch == target && backslashes % 2 == 0)
                return i - _index;
            backslashes = 0;
            i++;
        }
        return _input.Length - _index;
    }

    private object? ParseJson()
    {
        while (true)
        {
            var ch = Peek();
            if (ch is null)
                return _context.IsEmpty ? null : "";

            if (ch == '{')
            {
                Advance();
                return ParseObject();
            }
            if (ch == '[')
            {
                Advance();
                return ParseArray();
            }
            if (!_context.IsEmpty && (ch == '"' || ch == '\'' || ch == '“' || ch == '”' || char.IsLetter(ch.Value)))
                return ParseString();
            if (!_context.IsEmpty && (char.IsDigit(ch.Value) || ch == '-' || ch == '.'))
                return ParseNumber();
            if (ch == '#' || ch == '/')
                return ParseComment();

            // 无法识别的字符，跳过
            Advance();
        }
    }
}
