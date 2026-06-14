namespace Novap.JsonRepair.Parsing;

internal sealed partial class JsonParser
{
    private Dictionary<string, object?> ParseObject()
    {
        var obj = new Dictionary<string, object?>();
        _context.Enter(ParseState.ObjectKey);
        try
        {
            while ((Peek() ?? '}') != '}')
            {
                SkipWhitespace();

                // 多余的冒号
                if (Peek() == ':')
                    Advance();

                // 解析键
                var key = ParseObjectKey();
                SkipWhitespace();

                // 到达末尾
                if ((Peek() ?? '}') == '}')
                    continue;

                // 缺少冒号 → 修复：继续解析值
                if (Peek() != ':')
                {
                    // 修复：缺少冒号，继续
                }
                else
                {
                    Advance(); // 跳过 :
                }

                // 解析值
                _context.Enter(ParseState.ObjectValue);
                object? value;
                try
                {
                    SkipWhitespace();
                    var ch = Peek();
                    if (ch is ',' or '}')
                    {
                        value = ""; // 缺少值
                    }
                    else
                    {
                        value = ParseJson();
                    }
                }
                finally
                {
                    _context.Exit();
                }

                obj[key] = value;

                // 跳过分隔符
                if (Peek() is ',' or '\'' or '"')
                    Advance();

                // 如果在数组上下文中遇到 ]
                if (Peek() == ']' && _context.Contains(ParseState.Array))
                {
                    _index--;
                    break;
                }

                SkipWhitespace();
            }

            // 跳过 }
            if (Peek() == '}')
                Advance();
        }
        finally
        {
            _context.Exit();
        }

        return obj;
    }

    private string ParseObjectKey()
    {
        SkipWhitespace();
        var ch = Peek();

        if (ch is null)
            return "";

        // 有引号的键
        if (StringDelimiters.Contains(ch.Value))
        {
            return (string)ParseString()!;
        }

        // 无引号的键（裸标识符）
        var start = _index;
        while (Peek() is { } c && (char.IsLetterOrDigit(c) || c == '_' || c == '$'))
            Advance();

        if (_index > start)
            return _input[start.._index];

        return "";
    }
}
