namespace Novap.JsonRepair.Parsing;

internal sealed partial class JsonParser
{
    private List<object?> ParseArray()
    {
        var arr = new List<object?>();
        _context.Enter(ParseState.Array);
        try
        {
            SkipWhitespace();
            SkipComments();
            var ch = Peek();

            while (ch is not null && ch != ']' && ch != '}')
            {
                // 字符串后跟 ':' → 可能是缺少 { 的对象
                if (StringDelimiters.Contains(ch.Value))
                {
                    var lookAhead = SkipToChar(ch.Value, 1);
                    var afterStr = ScrollWhitespace(lookAhead + 1);
                    if (Peek(afterStr) == ':')
                    {
                        arr.Add(ParseObject());
                    }
                    else
                    {
                        arr.Add(ParseString());
                    }
                }
                else
                {
                    arr.Add(ParseJson());
                }

                ch = Peek();
                // 跳过逗号、空白和注释
                while (ch is not null && ch != ']' && (char.IsWhiteSpace(ch.Value) || ch == ','))
                {
                    Advance();
                    ch = Peek();
                }
                if (ch is '#' or '/')
                {
                    SkipComments();
                    ch = Peek();
                }
            }

            if (ch == ']')
                Advance(); // 跳过 ]
            // 缺少闭合 ]，自动补全（什么都不做）
        }
        finally
        {
            _context.Exit();
        }

        return arr;
    }

    /// <summary>
    /// 跳过当前位置的所有连续注释（#、//、/* */）。
    /// </summary>
    private void SkipComments()
    {
        while (Peek() is '#' or '/')
        {
            var saved = _index;
            ParseComment();
            SkipWhitespace();
            // 防止无限循环：如果注释解析没有推进索引，退出
            if (_index == saved) break;
        }
    }
}
