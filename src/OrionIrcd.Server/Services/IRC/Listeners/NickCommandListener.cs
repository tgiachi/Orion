using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class NickCommandListener : IIrcCommandListener<NickCommand>
{
    private readonly IEventBus _eventBus;
    private readonly IIrcReplyService _replyService;
    private readonly IIrcSessionStateService _stateService;

    public NickCommandListener(
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
        IrcCommandListenerContext<NickCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Command.Nickname))
        {
            await _replyService.SendNumericAsync(
                context.Session,
                "431",
                "*",
                "No nickname given",
                cancellationToken
            ).ConfigureAwait(false);

            return;
        }

        if (!_stateService.TrySetNickname(context.Session.SessionId, context.Command.Nickname))
        {
            await _replyService.SendLineAsync(
                context.Session,
                $":{_replyService.ServerName} 433 * {context.Command.Nickname} :Nickname is already in use",
                cancellationToken
            ).ConfigureAwait(false);

            return;
        }

        await TryCompleteRegistrationAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private async Task TryCompleteRegistrationAsync(
        IrcCommandListenerContext<NickCommand> context,
        CancellationToken cancellationToken
    )
    {
        if (!_stateService.TryMarkRegistered(context.Session.SessionId, out var snapshot) || snapshot is null)
        {
            return;
        }

        await _replyService.SendNumericAsync(
            context.Session,
            "001",
            snapshot.Nickname,
            $"Welcome to OrionIRCd {snapshot.Nickname}",
            cancellationToken
        ).ConfigureAwait(false);

        await _eventBus.PublishAsync(new IrcSessionRegisteredEvent(context.Session, snapshot), cancellationToken)
                       .ConfigureAwait(false);
    }
}
