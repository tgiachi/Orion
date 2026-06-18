using DryIoc;
using OrionIrcd.Core.Container;
using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;
using OrionIrcd.IRC.Services;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Extensions.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Interfaces.Services;
using OrionIrcd.Server.Services.IRC;
using OrionIrcd.Server.Services.Listeners;
using OrionIrcd.Server.Services.Sessions;
using OrionIrcd.Tests.Support.Events;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.IRC;

public class IrcCommandPipelineServiceTests
{
    [Fact]
    public async Task HandleAsync_WithInvalidLine_DoesNotDispatch()
    {
        using var container = new Container();
        RecordingNickListener.Contexts.Clear();
        container.RegisterIrcCommandList<NickCommand, RecordingNickListener>();
        var sessionManager = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        sessionManager.Register(connection);
        var service = CreateService(container, sessionManager);

        await service.HandleAsync(new NetworkResultReceivedEvent<string>(connection, "   "), CancellationToken.None);

        Assert.Empty(RecordingNickListener.Contexts);
    }

    [Fact]
    public async Task HandleAsync_WithKnownCommand_DispatchesTypedCommand()
    {
        using var container = new Container();
        RecordingNickListener.Contexts.Clear();
        container.RegisterIrcCommandList<NickCommand, RecordingNickListener>();
        var sessionManager = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = sessionManager.Register(connection);
        var service = CreateService(container, sessionManager);

        await service.HandleAsync(new NetworkResultReceivedEvent<string>(connection, "NICK squid"), CancellationToken.None);

        var context = Assert.Single(RecordingNickListener.Contexts);
        Assert.Same(session, context.Session);
        Assert.Equal("squid", context.Command.Nickname);
    }

    private static IrcCommandPipelineService CreateService(IContainer container, ISessionManagerService sessionManager)
    {
        var registry = new IrcCommandRegistry();
        registry.RegisterCommand<NickCommand>(
            (command, raw) => command.Nickname = raw.Params.Count > 0 ? raw.Params[0] : string.Empty
        );
        var dispatcher = new IrcCommandDispatcherService(
            container.Resolve<List<IrcCommandDispatchRegistration>>(),
            container
        );

        return new(
            new IrcMessageParser(),
            new IrcCommandFactory(registry, new(registry)),
            dispatcher,
            sessionManager
        );
    }

    private sealed class RecordingNickListener : IIrcCommandListener<NickCommand>
    {
        public static List<IrcCommandListenerContext<NickCommand>> Contexts { get; } = [];

        public ValueTask HandleCommandAsync(
            IrcCommandListenerContext<NickCommand> context,
            CancellationToken cancellationToken = default
        )
        {
            Contexts.Add(context);

            return ValueTask.CompletedTask;
        }
    }
}
