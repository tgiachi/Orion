using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OrionIrcd.Tests.Support.Network;

public static class TestCertificateFactory
{
    public static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=localhost",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false
            )
        );

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1)
        );

        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx),
            string.Empty,
            X509KeyStorageFlags.EphemeralKeySet
        );
    }
}
