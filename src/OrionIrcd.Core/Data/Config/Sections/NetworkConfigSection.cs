namespace OrionIrcd.Core.Data.Config.Sections;

public class NetworkConfigSection
{
    public string SSLCertFile { get; set; }

    public string SSLCertPassword { get; set; }

    public List<NetworkSectionEntry> Entries { get; set; } = [];
}
