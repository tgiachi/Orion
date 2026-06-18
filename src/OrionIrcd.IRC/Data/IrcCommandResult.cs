namespace OrionIrcd.IRC.Data;

public sealed record IrcCommandResult<T>
{
    public T? Value { get; init; }

    public IrcCommandError? Error { get; init; }

    public bool IsSuccess => Error is null;

    public static IrcCommandResult<T> Success(T value)
    {
        return new() { Value = value };
    }

    public static IrcCommandResult<T> Failure(IrcCommandError error)
    {
        return new() { Error = error };
    }
}
