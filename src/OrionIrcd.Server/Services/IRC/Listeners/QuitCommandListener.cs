using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Core.Interfaces.Services;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class QuitCommandListener : IIrcCommandListener<QuitCommand>
{
    private readonly IIrcReplyService _replyService;
    private readonly ISessionManagerService _sessionManagerService;

    public QuitCommandListener(IIrcReplyService replyService, ISessionManagerService sessionManagerService)
    {
        _replyService = replyService;
        _sessionManagerService = sessionManagerService;
    }

    public async ValueTask HandleCommandAsync(
        IrcCommandListenerContext<QuitCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reason = string.IsNullOrWhiteSpace(context.Command.Reason)
                         ? "Client Quit"
                         : context.Command.Reason;

        await _replyService.SendReplyAsync(
            context.Session,
            IrcReplies.ClosingLink(reason),
            cancellationToken
        );

        await _sessionManagerService.CloseAsync(context.Session.SessionId, cancellationToken);
    }
}
