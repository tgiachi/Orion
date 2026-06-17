using System.Net;
using System.Net.Sockets;
using OrionIrcd.Core.Utils;

namespace OrionIrcd.Tests.Core.Utils;

public class NetworkUtilsTests
{
    [Fact]
    public void ParsePorts_RangeAndSinglePorts_ReturnsExpandedPortList()
    {
        var ports = NetworkUtils.ParsePorts("6666-6668,6669,8000");

        Assert.Equal([6666, 6667, 6668, 6669, 8000], ports);
    }

    [Fact]
    public void ParseIpAddress_Wildcard_ReturnsAnyAddress()
    {
        var ipAddress = NetworkUtils.ParseIpAddress("*");

        Assert.Equal(IPAddress.Any, ipAddress);
    }

    [Fact]
    public void ParseIpAddress_IpV4Address_ReturnsParsedAddress()
    {
        var ipAddress = NetworkUtils.ParseIpAddress("10.0.0.1");

        Assert.Equal(IPAddress.Parse("10.0.0.1"), ipAddress);
    }

    [Fact]
    public void GetListeningAddresses_IpV4Endpoint_ReturnsMatchingEndpointFamilyAndPort()
    {
        var addresses = NetworkUtils.GetListeningAddresses(new IPEndPoint(IPAddress.Any, 6667)).ToArray();

        Assert.NotEmpty(addresses);
        Assert.All(addresses, address =>
        {
            Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily);
            Assert.Equal(6667, address.Port);
        });
    }
}
