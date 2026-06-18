using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class PingCommandListener : IIrcCommandListener<PingCommand>
{
    private readonly IIrcReplyService _replyService;

    public PingCommandListener(IIrcReplyService replyService)
    {
        _replyService = replyService;
    }

    public async ValueTask HandleCommandAsync(
        IrcCommandListenerContext<PingCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token = string.IsNullOrWhiteSpace(context.Command.Token)
                        ? _replyService.ServerName
                        : context.Command.Token;

        await _replyService.SendReplyAsync(
            context.Session,
            IrcReplies.Pong(token),
            cancellationToken
        ).ConfigureAwait(false);
    }
}
