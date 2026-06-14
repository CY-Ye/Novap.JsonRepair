namespace Novap.JsonRepair.Parsing;

internal sealed partial class JsonParser
{
    private object? ParseComment()
    {
        while (true)
        {
            var ch = Peek();
            if (ch is null)
                return "";

            // # 单行注释
            if (ch == '#')
            {
                while (Peek() is { } c && c is not '\n' and not '\r')
                    Advance();
            }
            else if (ch == '/')
            {
                var next = Peek(1);
                if (next == '/')
                {
                    Advance(); Advance();
                    while (Peek() is { } c && c is not '\n' and not '\r')
                        Advance();
                }
                else if (next == '*')
                {
                    Advance(); Advance();
                    while (true)
                    {
                        ch = Peek();
                        if (ch is null) break;
                        Advance();
                        if (ch == '*' && Peek() == '/')
                        {
                            Advance();
                            break;
                        }
                    }
                }
                else
                {
                    Advance();
                }
            }

            if (_context.IsEmpty)
            {
                SkipWhitespace();
                if (Peek() is '#' or '/')
                    continue;
                return ParseJson();
            }

            break;
        }

        return "";
    }
}
