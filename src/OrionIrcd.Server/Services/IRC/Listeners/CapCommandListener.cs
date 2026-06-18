using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class CapCommandListener : IIrcCommandListener<CapCommand>
{
    private readonly IIrcReplyService _replyService;

    public CapCommandListener(IIrcReplyService replyService)
    {
        _replyService = replyService;
    }

    public async ValueTask HandleCommandAsync(
        IrcCommandListenerContext<CapCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(context.Command.Subcommand, "LS", StringComparison.OrdinalIgnoreCase))
        {
            await _replyService.SendReplyAsync(
                context.Session,
                IrcReplies.CapabilityList(),
                cancellationToken
            );

            return;
        }

        if (string.Equals(context.Command.Subcommand, "REQ", StringComparison.OrdinalIgnoreCase))
        {
            await _replyService.SendReplyAsync(
                context.Session,
                IrcReplies.CapabilityNak(context.Command.Capabilities),
                cancellationToken
            );
        }
    }
}
