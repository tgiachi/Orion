using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Utils;
using OrionIrcd.Core.Yaml;

namespace OrionIrcd.Tests.Core.Yaml;

public class YamlUtilsTests
{
    [Fact]
    public void Deserialize_ConfigWithEnumNames_ReadsNetworkEntries()
    {
        var passwordHash = HashUtils.HashPassword("server-secret");
        var yaml = $$"""
                     ServerName: irc.orionircd.net
                     NetworkName: irc.orionircd.net
                     Pass: {{passwordHash}}
                     MOTD: Welcome to OrionIRCd
                     Logging:
                       LogToConsole: true
                       LogToFile: false
                       LogLevel: Information
                     Network:
                       Entries:
                         - IpAddress: '*'
                           Ports: 6666-6668
                           Type: TCP
                           Protocol: Plain
                           Mode: Server
                     """;

        var config = YamlUtils.Deserialize<OrionIrcdConfig>(yaml);

        var entry = Assert.Single(config.Network.Entries);
        Assert.True(HashUtils.VerifyPassword("server-secret", config.Pass));
        Assert.Equal("Welcome to OrionIRCd", config.MOTD);
        Assert.Equal("*", entry.IpAddress);
        Assert.Equal(ServerType.TCP, entry.Type);
        Assert.Equal(ServerProtocolType.Plain, entry.Protocol);
        Assert.Equal(ServerModeType.Server, entry.Mode);
    }

    [Fact]
    public void Serialize_ConfigWithNetworkEntries_WritesFormattedYaml()
    {
        var config = new OrionIrcdConfig();
        config.Pass = HashUtils.HashPassword("server-secret");
        config.MOTD = "file://motd.txt";

        config.Network.Entries.Add(
            new()
            {
                IpAddress = "*",
                Mode = ServerModeType.Server,
                Ports = "6666-6668",
                Protocol = ServerProtocolType.Plain,
                Type = ServerType.TCP
            }
        );

        var yaml = YamlUtils.Serialize(config);

        Assert.Contains("ServerName: irc.orionircd.net", yaml);
        Assert.Contains("Pass: pbkdf2-sha256$", yaml);
        Assert.Contains("MOTD: file://motd.txt", yaml);
        Assert.DoesNotContain("server-secret", yaml);
        Assert.Contains("Network:", yaml);
        Assert.Contains("Entries:", yaml);
        Assert.Contains("- IpAddress: '*'", yaml);
        Assert.Contains("Type: TCP", yaml);
        Assert.DoesNotContain("Network.Entries", yaml);
    }
}
