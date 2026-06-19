using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class NickCommandListener : IIrcCommandListener<NickCommand>
{
    private readonly IIrcRegistrationService _registrationService;
    private readonly IIrcReplyService _replyService;
    private readonly IIrcSessionStateService _stateService;

    public NickCommandListener(
        IIrcSessionStateService stateService,
        IIrcReplyService replyService,
        IIrcRegistrationService registrationService
    )
    {
        _stateService = stateService;
        _replyService = replyService;
        _registrationService = registrationService;
    }

    public async ValueTask HandleCommandAsync(
        IrcCommandListenerContext<NickCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Command.Nickname))
        {
            await _replyService.SendReplyAsync(
                context.Session,
                IrcReplies.NoNicknameGiven(),
                cancellationToken
            );

            return;
        }

        if (!_stateService.TrySetNickname(context.Session.SessionId, context.Command.Nickname))
        {
            await _replyService.SendReplyAsync(
                context.Session,
                IrcReplies.NicknameInUse(context.Command.Nickname),
                cancellationToken
            );

            return;
        }

        await _registrationService.TryCompleteRegistrationAsync(context.Session, cancellationToken);
    }
}
