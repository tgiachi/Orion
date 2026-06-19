using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Core.Data.Events;
using OrionIrcd.Server.Core.Interfaces.Services;
using OrionIrcd.Server.Interfaces.Services;
using Serilog;

namespace OrionIrcd.Server.Services.IRC;

public sealed class IrcCommandPipelineService : IAsyncEventListener<NetworkResultReceivedEvent<string>>
{
    private readonly IIrcCommandDispatcherService _dispatcherService;
    private readonly IIrcCommandFactory _factory;
    private readonly ILogger _logger = Log.ForContext<IrcCommandPipelineService>();
    private readonly IIrcMessageParser _parser;
    private readonly ISessionManagerService _sessionManagerService;

    public IrcCommandPipelineService(
        IIrcMessageParser parser,
        IIrcCommandFactory factory,
        IIrcCommandDispatcherService dispatcherService,
        ISessionManagerService sessionManagerService
    )
    {
        _parser = parser;
        _factory = factory;
        _dispatcherService = dispatcherService;
        _sessionManagerService = sessionManagerService;
    }

    public async Task HandleAsync(
        NetworkResultReceivedEvent<string> eventData,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_sessionManagerService.TryGetSession(eventData.Connection.SessionId, out var session) || session is null)
        {
            session = _sessionManagerService.Register(eventData.Connection);
        }

        var rawMessage = _parser.ParseMessage(eventData.Result);

        if (rawMessage is null)
        {
            return;
        }

        var command = _factory.CreateOrFallback(rawMessage);

        _logger.Debug(
            "Dispatching IRC command {CommandCode} for session {SessionId}",
            command.Code,
            session.SessionId
        );

        await _dispatcherService.DispatchAsync(session, command, cancellationToken);
    }
}
