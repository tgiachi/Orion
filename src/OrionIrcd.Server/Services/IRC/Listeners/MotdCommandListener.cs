using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;

namespace OrionIrcd.Server.Services.IRC.Listeners;

public sealed class MotdCommandListener : IIrcCommandListener<MotdCommand>
{
    private const string DefaultTarget = "*";

    private readonly IIrcMotdService _motdService;
    private readonly IIrcSessionStateService _stateService;

    public MotdCommandListener(IIrcSessionStateService stateService, IIrcMotdService motdService)
    {
        _stateService = stateService;
        _motdService = motdService;
    }

    public async ValueTask HandleCommandAsync(
        IrcCommandListenerContext<MotdCommand> context,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = _stateService.GetSnapshot(context.Session.SessionId);
        var target = string.IsNullOrWhiteSpace(snapshot.Nickname) ? DefaultTarget : snapshot.Nickname;

        await _motdService.SendMotdAsync(context.Session, target, cancellationToken);
    }
}
