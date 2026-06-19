using System.Text;
using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Utils;
using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Data.Events;
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

    [Fact]
    public async Task NickCommandListener_WithDuplicateNickname_SendsNicknameInUseError()
    {
        var context = CreateContext(new NickCommand { Nickname = "squid" }, out var connection);
        var stateService = new IrcSessionStateService();
        var config = CreateConfig();
        var replyService = CreateReplyService(context.SessionManager, config);
        var registrationService = CreateRegistrationService(
            context.SessionManager,
            stateService,
            replyService,
            config
        );
        stateService.TrySetNickname(99, "squid");
        var listener = new NickCommandListener(
            stateService,
            replyService,
            registrationService
        );

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);

        Assert.Equal(":orionircd 433 * squid :Nickname is already in use\r\n", ReadSinglePayload(connection));
    }

    [Fact]
    public async Task NickCommandListener_WithEmptyNickname_SendsNoNicknameError()
    {
        var context = CreateContext(new NickCommand { Nickname = string.Empty }, out var connection);
        var config = CreateConfig();
        var stateService = new IrcSessionStateService();
        var replyService = CreateReplyService(context.SessionManager, config);
        var listener = new NickCommandListener(
            stateService,
            replyService,
            CreateRegistrationService(context.SessionManager, stateService, replyService, config)
        );

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);

        Assert.Equal(":orionircd 431 * :No nickname given\r\n", ReadSinglePayload(connection));
    }

    [Fact]
    public async Task UserCommandListener_WhenNickAlreadySet_CompletesRegistrationAndSendsWelcome()
    {
        var context = CreateContext(
            new UserCommand
            {
                Username = "squiduser",
                RealName = "Squid User"
            },
            out var connection
        );
        var eventBus = new RecordingEventBus();
        var stateService = new IrcSessionStateService();
        var config = CreateConfig();
        var replyService = CreateReplyService(context.SessionManager, config);
        stateService.TrySetNickname(context.ListenerContext.Session.SessionId, "squid");
        var listener = new UserCommandListener(
            stateService,
            replyService,
            CreateRegistrationService(context.SessionManager, stateService, replyService, config, eventBus)
        );

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);
        var registeredEvent = await eventBus.WaitForEventAsync<IrcSessionRegisteredEvent>(TimeSpan.FromSeconds(5));
        var payloads = connection.SentPayloads.Select(Encoding.UTF8.GetString).ToArray();

        Assert.Equal(":orionircd 001 squid :Welcome to OrionIRCd squid\r\n", payloads[0]);
        Assert.Contains(payloads, payload => payload.StartsWith(":orionircd 005 squid CHANTYPES=# NICKLEN=30", StringComparison.Ordinal));
        Assert.Contains(payloads, payload => payload == ":orionircd 422 squid :MOTD File is missing\r\n");
        Assert.Equal("squid", registeredEvent.State.Nickname);
        Assert.Equal("squiduser", registeredEvent.State.Username);
    }

    [Fact]
    public async Task UserCommandListener_WithMissingUsername_SendsNeedMoreParams()
    {
        var context = CreateContext(new UserCommand(), out var connection);
        var config = CreateConfig();
        var stateService = new IrcSessionStateService();
        var replyService = CreateReplyService(context.SessionManager, config);
        var listener = new UserCommandListener(
            stateService,
            replyService,
            CreateRegistrationService(context.SessionManager, stateService, replyService, config)
        );

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);

        Assert.Equal(":orionircd 461 * USER :Not enough parameters\r\n", ReadSinglePayload(connection));
    }

    [Fact]
    public async Task PassCommandListener_WithHashedConfigPass_AcceptsPlainPassword()
    {
        var context = CreateContext(new PassCommand { Password = "server-secret" }, out var connection);
        var config = CreateConfig();
        config.Pass = HashUtils.HashPassword("server-secret");
        var stateService = new IrcSessionStateService();
        var replyService = CreateReplyService(context.SessionManager, config);
        var listener = new PassCommandListener(
            stateService,
            replyService,
            config,
            CreateRegistrationService(context.SessionManager, stateService, replyService, config)
        );

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);
        var snapshot = stateService.GetSnapshot(context.ListenerContext.Session.SessionId);

        Assert.True(snapshot.IsPassAccepted);
        Assert.Empty(connection.SentPayloads);
    }

    [Fact]
    public async Task PassCommandListener_WithPlainConfigPass_SendsPasswordMismatch()
    {
        var context = CreateContext(new PassCommand { Password = "server-secret" }, out var connection);
        var config = CreateConfig();
        config.Pass = "server-secret";
        var stateService = new IrcSessionStateService();
        var replyService = CreateReplyService(context.SessionManager, config);
        var listener = new PassCommandListener(
            stateService,
            replyService,
            config,
            CreateRegistrationService(context.SessionManager, stateService, replyService, config)
        );

        await listener.HandleCommandAsync(context.ListenerContext, CancellationToken.None);

        Assert.Equal(":orionircd 464 * :Password incorrect\r\n", ReadSinglePayload(connection));
    }

    private static IrcReplyService CreateReplyService(
        SessionManagerService sessionManagerService,
        OrionIrcdConfig? config = null
    )
        => new(
            sessionManagerService,
            config ?? CreateConfig()
        );

    private static OrionIrcdConfig CreateConfig()
        => new() { ServerName = "orionircd" };

    private static IrcRegistrationService CreateRegistrationService(
        SessionManagerService sessionManagerService,
        IrcSessionStateService stateService,
        IrcReplyService replyService,
        OrionIrcdConfig config,
        RecordingEventBus? eventBus = null
    )
    {
        var motdService = new IrcMotdService(
            replyService,
            config,
            new DirectoriesConfig(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), Enum.GetNames<DirectoryType>())
        );

        return new(
            stateService,
            replyService,
            motdService,
            config,
            eventBus ?? new RecordingEventBus()
        );
    }

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
