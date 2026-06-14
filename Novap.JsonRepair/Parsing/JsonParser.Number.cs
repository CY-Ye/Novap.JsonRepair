using System.Text;

namespace Novap.JsonRepair.Parsing;

internal sealed partial class JsonParser
{
    private static readonly HashSet<char> NumberChars = new("0123456789-.eE+_");

    private object? ParseNumber()
    {
        var sb = new StringBuilder();
        var isArray = _context.Current == ParseState.Array;

        char? ch;
        while ((ch = Peek()) is not null && NumberChars.Contains(ch.Value) && (!isArray || ch != ','))
        {
            if (ch != '_')
                sb.Append(ch);
            Advance();
        }

        // 数字后跟字母 → 实际是字符串
        if (Peek() is { } next && char.IsLetter(next))
        {
            _index -= sb.Length;
            return ParseString();
        }

        var numberStr = sb.ToString();

        // 移除无效尾字符
        if (numberStr.Length > 0 && numberStr[^1] is '-' or 'e' or 'E' or '/')
        {
            numberStr = numberStr[..^1];
            _index--;
        }

        if (numberStr.Length == 0)
            return 0L;

        if (numberStr.Contains(','))
            return numberStr;

        if (numberStr.Contains('.') || numberStr.Contains('e') || numberStr.Contains('E'))
        {
            if (double.TryParse(numberStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;
            return numberStr;
        }

        if (long.TryParse(numberStr, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var l))
            return l;

        return numberStr;
    }
}
