using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class PongCommandListener : IIrcCommandListener<PongCommand>
{
    public ValueTask HandleCommandAsync(
        IrcCommandListenerContext<PongCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }
}
