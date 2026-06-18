namespace OrionIrcd.Server.Data.Listeners;

internal sealed class IrcCommandDispatchInvocation
{
    public IrcCommandDispatchInvocation(
        object listener,
        Type commandType,
        Func<CancellationToken, ValueTask> handleAsync
    )
    {
        Listener = listener;
        CommandType = commandType;
        HandleAsync = handleAsync;
    }

    public object Listener { get; }

    public Type CommandType { get; }

    public Func<CancellationToken, ValueTask> HandleAsync { get; }
}
