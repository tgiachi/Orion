using OrionIrcd.Core.Types;

namespace OrionIrcd.Core.Data.Config.Sections;

public class NetworkSectionEntry
{
    public string IpAddress { get; set; }

    public string Ports { get; set; }

    public ServerType Type { get; set; }

    public ServerProtocolType Protocol { get; set; }

    public ServerModeType Mode { get; set; }
}
