using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Core.Utils;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Data.IRC.Replies;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC;

public sealed class IrcRegistrationService : IIrcRegistrationService
{
    private const string CreatedMessage = "This server was created for OrionIRCd";

    private readonly OrionIrcdConfig _config;
    private readonly IEventBus _eventBus;
    private readonly IIrcMotdService _motdService;
    private readonly IIrcReplyService _replyService;
    private readonly IIrcSessionStateService _stateService;

    public IrcRegistrationService(
        IIrcSessionStateService stateService,
        IIrcReplyService replyService,
        IIrcMotdService motdService,
        OrionIrcdConfig config,
        IEventBus eventBus
    )
    {
        _stateService = stateService;
        _replyService = replyService;
        _motdService = motdService;
        _config = config;
        _eventBus = eventBus;
    }

    public async Task<bool> TryCompleteRegistrationAsync(NetworkSession session, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_stateService.TryMarkRegistered(session.SessionId, IsPassRequired(), out var snapshot) || snapshot is null)
        {
            return false;
        }

        var version = VersionUtils.GetVersion();

        await _replyService.SendReplyAsync(session, IrcReplies.Welcome(snapshot.Nickname), cancellationToken);
        await _replyService.SendReplyAsync(session, IrcReplies.YourHost(snapshot.Nickname, version), cancellationToken);
        await _replyService.SendReplyAsync(session, IrcReplies.Created(snapshot.Nickname, CreatedMessage), cancellationToken);
        await _replyService.SendReplyAsync(session, IrcReplies.MyInfo(snapshot.Nickname, version), cancellationToken);
        await _replyService.SendReplyAsync(session, IrcReplies.ISupport(snapshot.Nickname), cancellationToken);
        await _motdService.SendMotdAsync(session, snapshot.Nickname, cancellationToken);
        await _eventBus.PublishAsync(new IrcSessionRegisteredEvent(session, snapshot), cancellationToken);

        return true;
    }

    private bool IsPassRequired()
        => !string.IsNullOrWhiteSpace(_config.Pass);
}
