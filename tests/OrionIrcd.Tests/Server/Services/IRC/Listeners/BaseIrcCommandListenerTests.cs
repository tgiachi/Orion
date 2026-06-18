using System.Text;
using OrionIrcd.Core.Data.Config;
using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Services.IRC;
using OrionIrcd.Server.Services.IRC.Listeners;
using OrionIrcd.Server.Services.Sessions;
using OrionIrcd.Tests.Support.Events;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.IRC.Listeners;

public class BaseIrcCommandListenerTests
{
    [Fact]
    public async Task CapCommandListener_WithLs_SendsEmptyCapabilities()
    {
        var context = CreateContext(new CapCommand { Subcommand = "LS", Capabilities = ["302"] }, out var connection);
        var listener = new CapCommandListener(CreateReplyService(context.SessionManager));

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);

        Assert.Equal(":orionircd CAP * LS :\r\n", ReadSinglePayload(connection));
    }

    [Fact]
    public async Task PingCommandListener_WithToken_SendsPong()
    {
        var context = CreateContext(new PingCommand { Token = "abc123" }, out var connection);
        var listener = new PingCommandListener(CreateReplyService(context.SessionManager));

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);

        Assert.Equal(":orionircd PONG orionircd :abc123\r\n", ReadSinglePayload(connection));
    }

    [Fact]
    public async Task PongCommandListener_WithToken_CompletesWithoutSending()
    {
        var context = CreateContext(new PongCommand { Token = "abc123" }, out var connection);
        var listener = new PongCommandListener();

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);

        Assert.Empty(connection.SentPayloads);
    }

    [Fact]
    public async Task QuitCommandListener_WithReason_SendsErrorAndCloses()
    {
        var context = CreateContext(new QuitCommand { Reason = "Client Quit" }, out var connection);
        var replyService = CreateReplyService(context.SessionManager);
        var listener = new QuitCommandListener(replyService, context.SessionManager);

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);

        Assert.Equal("ERROR :Closing Link: Client Quit\r\n", ReadSinglePayload(connection));
        Assert.False(connection.IsConnected);
        Assert.Equal(1, connection.CloseCallCount);
    }

    private static IrcReplyService CreateReplyService(SessionManagerService sessionManagerService)
        => new(
            sessionManagerService,
            new IrcServerInfoService(
                new OrionIrcdConfig
                {
                    ServerName = "orionircd"
                }
            )
        );

    private static ListenerTestContext<TCommand> CreateContext<TCommand>(
        TCommand command,
        out TestNetworkConnection connection
    )
        where TCommand : IIrcCommand
    {
        var sessionManager = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        connection = new TestNetworkConnection { SessionId = 10 };
        var session = sessionManager.Register(connection);

        return new(
            sessionManager,
            new(session, command)
        );
    }

    private static string ReadSinglePayload(TestNetworkConnection connection)
        => Encoding.UTF8.GetString(Assert.Single(connection.SentPayloads));

    private sealed class ListenerTestContext<TCommand>
        where TCommand : IIrcCommand
    {
        public ListenerTestContext(
            SessionManagerService sessionManager,
            IrcCommandListenerContext<TCommand> listenerContext
        )
        {
            SessionManager = sessionManager;
            ListenerContext = listenerContext;
        }

        public SessionManagerService SessionManager { get; }

        public IrcCommandListenerContext<TCommand> ListenerContext { get; }
    }
}
