using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class UserCommandListener : IIrcCommandListener<UserCommand>
{
    private readonly IEventBus _eventBus;
    private readonly IIrcReplyService _replyService;
    private readonly IIrcSessionStateService _stateService;

    public UserCommandListener(
        IIrcSessionStateService stateService,
        IIrcReplyService replyService,
        IEventBus eventBus
    )
    {
        _stateService = stateService;
        _replyService = replyService;
        _eventBus = eventBus;
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
            ).ConfigureAwait(false);

            return;
        }

        _stateService.SetUser(
            context.Session.SessionId,
            context.Command.Username,
            context.Command.RealName
        );

        await TryCompleteRegistrationAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private async Task TryCompleteRegistrationAsync(
        IrcCommandListenerContext<UserCommand> context,
        CancellationToken cancellationToken
    )
    {
        if (!_stateService.TryMarkRegistered(context.Session.SessionId, out var snapshot) || snapshot is null)
        {
            return;
        }

        await _replyService.SendReplyAsync(
            context.Session,
            IrcReplies.Welcome(snapshot.Nickname),
            cancellationToken
        ).ConfigureAwait(false);

        await _eventBus.PublishAsync(new IrcSessionRegisteredEvent(context.Session, snapshot), cancellationToken)
                       .ConfigureAwait(false);
    }
}
