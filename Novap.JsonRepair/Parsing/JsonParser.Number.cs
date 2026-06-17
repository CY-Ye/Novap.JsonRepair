using System.Globalization;

namespace Novap.JsonRepair.Parsing;

internal sealed partial class JsonParser
{
    private static bool IsNumberChar(char c) => c is (>= '0' and <= '9') or '-' or '.' or 'e' or 'E' or '+' or '_';

    private object? ParseNumber()
    {
        var isArray = _context.Current == ParseState.Array;
        var start = _index;
        var hasUnderscore = false;

        char? ch;
        while ((ch = Peek()) is not null && IsNumberChar(ch.Value) && (!isArray || ch != ','))
        {
            if (ch == '_') hasUnderscore = true;
            Advance();
        }

        var length = _index - start;

        // 数字后跟字母 → 实际是字符串
        if (Peek() is { } next && char.IsLetter(next))
        {
            _index = start;
            return ParseString();
        }

        // 移除无效尾字符
        if (length > 0 && _input[start + length - 1] is '-' or 'e' or 'E' or '/')
        {
            length--;
            _index--;
        }

        if (length == 0)
            return 0L;

        ReadOnlySpan<char> span = _input.AsSpan(start, length);

        // 有下划线则需要清理（Python 风格 1_000）
        if (hasUnderscore)
        {
            // 极少走到这里，直接分配字符串去下划线
            var cleaned = new string(span).Replace("_", "");
            span = cleaned.AsSpan();
        }

        if (span.Contains(','))
            return span.ToString();

        if (span.Contains('.') || span.Contains('e') || span.Contains('E'))
        {
            if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
            return span.ToString();
        }

        if (long.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            return l;

        return span.ToString();
    }
}
