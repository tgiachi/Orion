using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class UserCommandListener : IIrcCommandListener<UserCommand>
{
    private readonly IIrcRegistrationService _registrationService;
    private readonly IIrcReplyService _replyService;
    private readonly IIrcSessionStateService _stateService;

    public UserCommandListener(
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
        IrcCommandListenerContext<UserCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Command.Username))
        {
            await _replyService.SendReplyAsync(
                context.Session,
                IrcReplies.NeedMoreParameters("USER"),
                cancellationToken
            );

            return;
        }

        _stateService.SetUser(
            context.Session.SessionId,
            context.Command.Username,
            context.Command.RealName
        );

        await _registrationService.TryCompleteRegistrationAsync(context.Session, cancellationToken);
    }
}
