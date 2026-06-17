using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Yaml;

namespace OrionIrcd.Tests.Core.Yaml;

public class YamlUtilsTests
{
    [Fact]
    public void Serialize_ConfigWithNetworkEntries_WritesFormattedYaml()
    {
        var config = new OrionIrcdConfig();

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
        Assert.Contains("Network:", yaml);
        Assert.Contains("Entries:", yaml);
        Assert.Contains("- IpAddress: '*'", yaml);
        Assert.Contains("Type: TCP", yaml);
        Assert.DoesNotContain("Network.Entries", yaml);
    }

    [Fact]
    public void Deserialize_ConfigWithEnumNames_ReadsNetworkEntries()
    {
        const string yaml = """
                            ServerName: irc.orionircd.net
                            NetworkName: irc.orionircd.net
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
        Assert.Equal("*", entry.IpAddress);
        Assert.Equal(ServerType.TCP, entry.Type);
        Assert.Equal(ServerProtocolType.Plain, entry.Protocol);
        Assert.Equal(ServerModeType.Server, entry.Mode);
    }
}
