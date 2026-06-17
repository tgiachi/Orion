using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace OrionIrcd.Network.Data.Options;

public sealed class OrionTcpServerTlsOptions
{
    public OrionTcpServerTlsOptions(X509Certificate2 serverCertificate)
    {
        ArgumentNullException.ThrowIfNull(serverCertificate);

        ServerCertificate = serverCertificate;
    }

    public X509Certificate2 ServerCertificate { get; }

    public bool ClientCertificateRequired { get; init; }

    public bool CheckCertificateRevocation { get; init; }

    public SslProtocols EnabledSslProtocols { get; init; } = SslProtocols.None;

    internal SslServerAuthenticationOptions ToAuthenticationOptions()
        => new()
        {
            ServerCertificate = ServerCertificate,
            ClientCertificateRequired = ClientCertificateRequired,
            CertificateRevocationCheckMode = CheckCertificateRevocation
                                                 ? X509RevocationMode.Online
                                                 : X509RevocationMode.NoCheck,
            EnabledSslProtocols = EnabledSslProtocols
        };
}
