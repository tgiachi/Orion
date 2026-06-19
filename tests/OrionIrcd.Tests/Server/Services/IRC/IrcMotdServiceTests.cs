using System.Text;
using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Types;
using OrionIrcd.Server.Core.Data.Sessions;
using OrionIrcd.Server.Services.IRC;
using OrionIrcd.Server.Core.Services.Sessions;
using OrionIrcd.Tests.Support.Events;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.IRC;

public class IrcMotdServiceTests
{
    [Fact]
    public async Task SendMotdAsync_WithInlineMotd_SendsMotdLines()
    {
        using var tempRoot = new TemporaryDirectory();
        var directories = new DirectoriesConfig(tempRoot.Path, Enum.GetNames<DirectoryType>());
        var (service, connection, session) = CreateService("Welcome\nEnjoy Orion", directories);

        await service.SendMotdAsync(session, "squid", CancellationToken.None);

        Assert.Equal(
            [
                ":orionircd 375 squid :- orionircd Message of the day -\r\n",
                ":orionircd 372 squid :- Welcome\r\n",
                ":orionircd 372 squid :- Enjoy Orion\r\n",
                ":orionircd 376 squid :End of /MOTD command.\r\n"
            ],
            connection.SentPayloads.Select(Encoding.UTF8.GetString).ToArray()
        );
    }

    [Fact]
    public async Task SendMotdAsync_WithFileMotd_LoadsFromDataDirectory()
    {
        using var tempRoot = new TemporaryDirectory();
        var directories = new DirectoriesConfig(tempRoot.Path, Enum.GetNames<DirectoryType>());
        File.WriteAllText(Path.Combine(directories[DirectoryType.Data], "motd.txt"), "From file");
        var (service, connection, session) = CreateService("file://motd.txt", directories);

        await service.SendMotdAsync(session, "squid", CancellationToken.None);

        Assert.Contains(
            connection.SentPayloads,
            payload => Encoding.UTF8.GetString(payload) == ":orionircd 372 squid :- From file\r\n"
        );
    }

    [Fact]
    public async Task SendMotdAsync_WithBlankMotd_SendsNoMotd()
    {
        using var tempRoot = new TemporaryDirectory();
        var directories = new DirectoriesConfig(tempRoot.Path, Enum.GetNames<DirectoryType>());
        var (service, connection, session) = CreateService(string.Empty, directories);

        await service.SendMotdAsync(session, "squid", CancellationToken.None);

        Assert.Equal(":orionircd 422 squid :MOTD File is missing\r\n", Encoding.UTF8.GetString(Assert.Single(connection.SentPayloads)));
    }

    [Fact]
    public async Task SendMotdAsync_WithMissingFileMotd_SendsNoMotd()
    {
        using var tempRoot = new TemporaryDirectory();
        var directories = new DirectoriesConfig(tempRoot.Path, Enum.GetNames<DirectoryType>());
        var (service, connection, session) = CreateService("file://missing.txt", directories);

        await service.SendMotdAsync(session, "squid", CancellationToken.None);

        Assert.Equal(":orionircd 422 squid :MOTD File is missing\r\n", Encoding.UTF8.GetString(Assert.Single(connection.SentPayloads)));
    }

    private static (IrcMotdService Service, TestNetworkConnection Connection, NetworkSession Session) CreateService(
        string motd,
        DirectoriesConfig directoriesConfig
    )
    {
        var config = new OrionIrcdConfig
        {
            ServerName = "orionircd",
            MOTD = motd
        };
        var connection = new TestNetworkConnection { SessionId = 10 };
        var sessionManager = new SessionManagerService(new RecordingEventBus(), TimeProvider.System);
        var session = sessionManager.Register(connection);
        var replyService = new IrcReplyService(sessionManager, config);

        return (new(replyService, config, directoriesConfig), connection, session);
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
