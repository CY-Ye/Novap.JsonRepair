using System.Text;

namespace Novap.JsonRepair.Parsing;

internal sealed partial class JsonParser
{
    private static readonly HashSet<char> StringDelimiters = new(['"', '\'', '“', '”']);

    private object? ParseString()
    {
        // 跳过注释
        var ch = Peek();
        if (ch is '#' or '/')
            return ParseComment();

        // 跳过不属于字符串的非字母数字字符
        while (ch is not null && !StringDelimiters.Contains(ch.Value) && !char.IsLetterOrDigit(ch.Value))
        {
            Advance();
            ch = Peek();
        }

        if (ch is null)
            return "";

        // 尝试布尔/Null 字面量
        if (_context.Current != ParseState.ObjectKey && char.IsLetter(ch.Value))
        {
            var savedIndex = _index;
            var literal = TryParseBooleanOrNull();
            // 如果 TryParseBooleanOrNull 消费了字符（index 变化），说明匹配成功
            if (_index != savedIndex)
                return literal;
        }

        // 确定字符串分隔符
        char lDelimiter, rDelimiter;
        bool missingQuotes = false;

        if (ch == '"')
        {
            lDelimiter = rDelimiter = '"';
            Advance();
        }
        else if (ch == '\'')
        {
            lDelimiter = rDelimiter = '\'';
            Advance();
        }
        else if (ch == '“')
        {
            lDelimiter = '“';
            rDelimiter = '”';
            Advance();
        }
        else if (char.IsLetterOrDigit(ch.Value))
        {
            missingQuotes = true;
            lDelimiter = rDelimiter = '"';
        }
        else
        {
            return "";
        }

        // 处理 "" (空字符串 — 双引号紧跟)
        if (!missingQuotes && Peek() == lDelimiter)
        {
            if (Peek(1) == lDelimiter)
            {
                // """..." — doubled quote, skip one
                Advance();
            }
            else if (_context.Current == ParseState.ObjectKey && Peek(1) == ':')
            {
                Advance();
                return "";
            }
            else if (_context.Current == ParseState.ObjectValue && Peek(1) is ',' or '}')
            {
                Advance();
                return "";
            }
            else if (_context.Current == ParseState.Array && Peek(1) is ',' or ']')
            {
                Advance();
                return "";
            }
        }

        // 扫描字符串体
        var sb = new StringBuilder();
        while (true)
        {
            ch = Peek();
            if (ch is null)
                break;

            // 无引号模式下的终止条件
            if (missingQuotes)
            {
                if (_context.Current == ParseState.ObjectKey && (ch == ':' || char.IsWhiteSpace(ch.Value)))
                    break;
                if (_context.Current == ParseState.ObjectValue && ch == '}')
                    break;
                if (_context.Current == ParseState.Array && ch is ']' or ',')
                    break;
            }

            // 到达右分隔符
            if (ch == rDelimiter && !missingQuotes)
            {
                Advance();
                break;
            }

            // 无引号模式下遇到逗号或容器闭合
            if (missingQuotes && ch is ',' or '}' or ']')
                break;

            // 转义字符
            if (ch == '\\' && !missingQuotes)
            {
                Advance();
                var escaped = Peek();
                if (escaped is null) break;
                sb.Append(DecodeEscape(escaped.Value));
                Advance();
                continue;
            }

            sb.Append(ch);
            Advance();
        }

        // 去除尾部空白（无引号模式）
        if (missingQuotes)
            return sb.ToString().TrimEnd();

        return sb.ToString();
    }

    private object? TryParseBooleanOrNull()
    {
        var remaining = _input.AsSpan(_index);

        // JSON literals
        if (remaining.Length >= 4 && remaining[0] == 'n' && remaining[1] == 'u' && remaining[2] == 'l' && remaining[3] == 'l')
        {
            _index += 4;
            return null;
        }
        if (remaining.StartsWith("true"))
        {
            _index += 4;
            return true;
        }
        if (remaining.StartsWith("false"))
        {
            _index += 5;
            return false;
        }

        // Python literals: None → null, True → true, False → false
        if (remaining.StartsWith("None"))
        {
            _index += 4;
            return null;
        }
        if (remaining.StartsWith("True"))
        {
            _index += 4;
            return true;
        }
        if (remaining.StartsWith("False"))
        {
            _index += 5;
            return false;
        }

        return null;
    }

    private static char DecodeEscape(char c) => c switch
    {
        'n' => '\n',
        'r' => '\r',
        't' => '\t',
        'b' => '\b',
        'f' => '\f',
        '0' => '\0',
        _ => c
    };
}
