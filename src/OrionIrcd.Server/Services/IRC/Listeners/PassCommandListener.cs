using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Core.Utils;
using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class PassCommandListener : IIrcCommandListener<PassCommand>
{
    private readonly OrionIrcdConfig _config;
    private readonly IEventBus _eventBus;
    private readonly IIrcReplyService _replyService;
    private readonly IIrcSessionStateService _stateService;

    public PassCommandListener(
        IIrcSessionStateService stateService,
        IIrcReplyService replyService,
        OrionIrcdConfig config,
        IEventBus eventBus
    )
    {
        _stateService = stateService;
        _replyService = replyService;
        _config = config;
        _eventBus = eventBus;
    }

    public async ValueTask HandleCommandAsync(
        IrcCommandListenerContext<PassCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Command.Password))
        {
            await _replyService.SendReplyAsync(
                context.Session,
                IrcReplies.NeedMoreParameters("PASS"),
                cancellationToken
            );

            return;
        }

        if (IsPassRequired() && !HashUtils.VerifyPassword(context.Command.Password, _config.Pass))
        {
            await _replyService.SendReplyAsync(
                context.Session,
                IrcReplies.PasswordMismatch(),
                cancellationToken
            );

            return;
        }

        _stateService.SetPassAccepted(context.Session.SessionId);

        await TryCompleteRegistrationAsync(context, cancellationToken);
    }

    private async Task TryCompleteRegistrationAsync(
        IrcCommandListenerContext<PassCommand> context,
        CancellationToken cancellationToken
    )
    {
        if (!_stateService.TryMarkRegistered(context.Session.SessionId, IsPassRequired(), out var snapshot) ||
            snapshot is null)
        {
            return;
        }

        await _replyService.SendReplyAsync(
            context.Session,
            IrcReplies.Welcome(snapshot.Nickname),
            cancellationToken
        );

        await _eventBus.PublishAsync(new IrcSessionRegisteredEvent(context.Session, snapshot), cancellationToken);
    }

    private bool IsPassRequired()
        => !string.IsNullOrWhiteSpace(_config.Pass);
}
