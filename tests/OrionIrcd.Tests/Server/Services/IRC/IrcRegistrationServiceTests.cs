using System.Text;
using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Types;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Services.IRC;
using OrionIrcd.Server.Services.Sessions;
using OrionIrcd.Tests.Support.Events;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.IRC;

public class IrcRegistrationServiceTests
{
    [Fact]
    public async Task TryCompleteRegistrationAsync_WithNickAndUser_SendsRegistrationNumericsAndMotd()
    {
        using var tempRoot = new TemporaryDirectory();
        var config = new OrionIrcdConfig
        {
            ServerName = "orionircd",
            MOTD = "Welcome"
        };
        var eventBus = new RecordingEventBus();
        var sessionManager = new SessionManagerService(eventBus, TimeProvider.System);
        var connection = new TestNetworkConnection { SessionId = 10 };
        var session = sessionManager.Register(connection);
        var stateService = new IrcSessionStateService();
        stateService.TrySetNickname(session.SessionId, "squid");
        stateService.SetUser(session.SessionId, "squiduser", "Squid User");
        var replyService = new IrcReplyService(sessionManager, config);
        var motdService = new IrcMotdService(
            replyService,
            config,
            new DirectoriesConfig(tempRoot.Path, Enum.GetNames<DirectoryType>())
        );
        var service = new IrcRegistrationService(stateService, replyService, motdService, config, eventBus);

        var completed = await service.TryCompleteRegistrationAsync(session, CancellationToken.None);

        var payloads = connection.SentPayloads.Select(Encoding.UTF8.GetString).ToArray();
        Assert.True(completed);
        Assert.Equal(":orionircd 001 squid :Welcome to OrionIRCd squid\r\n", payloads[0]);
        Assert.StartsWith(":orionircd 002 squid :Your host is orionircd, running version ", payloads[1]);
        Assert.StartsWith(":orionircd 003 squid :", payloads[2]);
        Assert.StartsWith(":orionircd 004 squid orionircd OrionIRCd ", payloads[3]);
        Assert.Equal(":orionircd 005 squid CHANTYPES=# NICKLEN=30 :are supported by this server\r\n", payloads[4]);
        Assert.Equal(":orionircd 375 squid :- orionircd Message of the day -\r\n", payloads[5]);
        Assert.Equal(":orionircd 372 squid :- Welcome\r\n", payloads[6]);
        Assert.Equal(":orionircd 376 squid :End of /MOTD command.\r\n", payloads[7]);
        Assert.Contains(eventBus.Events, eventData => eventData is IrcSessionRegisteredEvent);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
