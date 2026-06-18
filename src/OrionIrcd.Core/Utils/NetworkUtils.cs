using System.Net;
using System.Net.NetworkInformation;

namespace OrionIrcd.Core.Utils;

/// <summary>
/// Utility methods for network configuration parsing.
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// Enumerates local unicast endpoints matching the supplied endpoint address family.
    /// </summary>
    /// <param name="endPoint">The template endpoint supplying address family and port.</param>
    /// <returns>The matching local endpoints.</returns>
    public static IEnumerable<IPEndPoint> GetListeningAddresses(IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        return NetworkInterface.GetAllNetworkInterfaces()
                               .SelectMany(
                                   adapter =>
                                       adapter.GetIPProperties()
                                              .UnicastAddresses
                                              .Where(unicast => endPoint.AddressFamily == unicast.Address.AddressFamily)
                                              .Select(unicast => new IPEndPoint(unicast.Address, endPoint.Port))
                               );
    }

    /// <summary>
    /// Parses an IP address, treating "*" as every IPv4 interface.
    /// </summary>
    /// <param name="ipAddress">The IP address or "*".</param>
    /// <returns>The parsed IP address.</returns>
    public static IPAddress ParseIpAddress(string ipAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);

        return ipAddress.Trim() == "*" ? IPAddress.Any : IPAddress.Parse(ipAddress);
    }

    /// <summary>
    /// Parses a comma-separated port list with optional ranges.
    /// </summary>
    /// <param name="ports">The ports string, such as "6666-6668,6669,8000".</param>
    /// <returns>The parsed ports.</returns>
    public static List<int> ParsePorts(string ports)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ports);

        var parsedPorts = new List<int>();

        foreach (var segment in ports.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (segment.Contains('-'))
            {
                AddPortRange(parsedPorts, segment);
            }
            else
            {
                parsedPorts.Add(ParsePort(segment));
            }
        }

        return parsedPorts;
    }

    private static void AddPortRange(List<int> ports, string range)
    {
        var rangeParts = range.Split('-', StringSplitOptions.TrimEntries);

        if (rangeParts.Length != 2)
        {
            throw new FormatException($"Invalid port range '{range}'.");
        }

        var startPort = ParsePort(rangeParts[0]);
        var endPort = ParsePort(rangeParts[1]);

        if (startPort > endPort)
        {
            throw new FormatException($"Invalid port range '{range}'.");
        }

        for (var port = startPort; port <= endPort; port++)
        {
            ports.Add(port);
        }
    }

    private static int ParsePort(string port)
    {
        if (!int.TryParse(port, out var parsedPort) || parsedPort is < 0 or > 65535)
        {
            throw new FormatException($"Invalid port '{port}'.");
        }

        return parsedPort;
    }
}
