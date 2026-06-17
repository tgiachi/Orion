using System.Security.Cryptography.X509Certificates;

namespace OrionIrcd.Network.Data.Options;

public sealed class OrionWebSocketServerTlsOptions
{
    public X509Certificate2 ServerCertificate { get; }

    public OrionWebSocketServerTlsOptions(X509Certificate2 serverCertificate)
    {
        ArgumentNullException.ThrowIfNull(serverCertificate);

        ServerCertificate = serverCertificate;
    }
}
