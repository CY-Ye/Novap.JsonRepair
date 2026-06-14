namespace Novap.JsonRepair.Parsing;

internal enum ParseState
{
    ObjectKey,
    ObjectValue,
    Array
}

internal sealed class ParseContext
{
    private readonly Stack<ParseState> _stack = new();

    public ParseState Current => _stack.Count > 0 ? _stack.Peek() : default;
    public bool IsEmpty => _stack.Count == 0;

    public void Enter(ParseState state) => _stack.Push(state);
    public void Exit() => _stack.Pop();
    public bool Contains(ParseState state) => _stack.Contains(state);
}
