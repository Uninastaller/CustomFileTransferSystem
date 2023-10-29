using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ConfigManager
{
    public static class Certificats
    {
        public enum CertificateType
        {
            Server,
            Client,
            Node
        }

        public static X509Certificate2 GetCertificate(string subjectName, CertificateType certType)
        {

            var fileName = $"{certType}Certificate.pfx";

            // Check if certificate already exists
            if (File.Exists(fileName))
            {
                Console.WriteLine($"Certificate {fileName} already exists. No new certificate will be created.");
                return new X509Certificate2(fileName, "password"); // Load the existing certificate
            }

            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                switch (certType)
                {
                    case CertificateType.Server:
                        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                        break;
                    case CertificateType.Client:
                        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));
                        break;
                    case CertificateType.Node:
                        // Potentially another OID for nodes or custom logic.
                        // If nodes don't require specific OIDs, you can omit this or add your custom logic.
                        break;
                }

                var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));
                var pfxBytes = certificate.Export(X509ContentType.Pfx, "password");

                File.WriteAllBytes(fileName, pfxBytes);

                return new X509Certificate2(pfxBytes, "password", X509KeyStorageFlags.Exportable);
            }
        }

        public static string ExtractPublicKey(X509Certificate2 certificate)
        {
            byte[] publicKeyBytes = certificate.PublicKey.EncodedKeyValue.RawData;
            return Convert.ToBase64String(publicKeyBytes);
        }
    }
}
