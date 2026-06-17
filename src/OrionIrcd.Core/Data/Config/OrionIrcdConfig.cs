using OrionIrcd.Core.Data.Config.Sections;

namespace OrionIrcd.Core.Data.Config;

public class OrionIrcdConfig
{
    public string ServerName { get; set; } = "irc.orionircd.net";

    public string NetworkName { get; set; } = "irc.orionircd.net";

    public LoggingSection Logging { get; set; } = new();

    public NetworkConfigSection Network { get; set; } = new();
}
