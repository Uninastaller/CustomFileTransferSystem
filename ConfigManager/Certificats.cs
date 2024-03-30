using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace ConfigManager
{
    public static class Certificats
    {
        public enum CertificateType
        {
            Server,
            Client,
            Node,
            CentralServer,
            ClientConnectionWithCentralServer
        }

        public static X509Certificate2 GetCertificate(string subjectName, CertificateType certType)
        {
            string certificateDirectory = MyConfigManager.GetConfigStringValue("CertificateDirectory");
            if (!string.IsNullOrEmpty(certificateDirectory))
            {
                if (!Directory.Exists(certificateDirectory))
                {
                    Directory.CreateDirectory(certificateDirectory);
                }
            }

            string filePath = Path.Combine(certificateDirectory, $"{certType}Certificate.pfx");

            // Check if certificate already exists
            if (File.Exists(filePath))
            {
                return new X509Certificate2(filePath, "password"); // Load the existing certificate
            }

            using (RSA rsa = RSA.Create(2048))
            {
                CertificateRequest request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                switch (certType)
                {
                    case CertificateType.Server:
                        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                        break;
                    case CertificateType.Client:
                        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));
                        break;
                    case CertificateType.CentralServer:
                        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                        break;
                    case CertificateType.ClientConnectionWithCentralServer:
                        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));
                        break;
                    case CertificateType.Node:
                        // Potentially another OID for nodes or custom logic.
                        // If nodes don't require specific OIDs, you can omit this or add your custom logic.
                        break;
                }

                X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));
                byte[] pfxBytes = certificate.Export(X509ContentType.Pfx, "password");

                try
                {
                    File.WriteAllBytes(filePath, pfxBytes);
                }
                catch
                {                    
                }

                return new X509Certificate2(pfxBytes, "password", X509KeyStorageFlags.Exportable);
            }
        }

        public static string ExtractPublicKey(X509Certificate2 certificate)
        {
            byte[] publicKeyBytes = certificate.PublicKey.EncodedKeyValue.RawData;
            return Convert.ToBase64String(publicKeyBytes);
        }

        public static string ExportPublicKeyToJSON(X509Certificate2 certificate)
        {
            RSA? rsa = certificate.GetRSAPublicKey();
            if (rsa == null)
            {
                return string.Empty;
            }
            RSAParameters rsaParameters = rsa.ExportParameters(false);

            if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
            {
                return string.Empty;
            }

            string json = JsonSerializer.Serialize(new RSAParametersJson()
            {
                Exponent = Convert.ToBase64String(rsaParameters.Exponent),
                Modulus = Convert.ToBase64String(rsaParameters.Modulus)
            });
            return json;
        }

        public static string SignString(X509Certificate2 certificate, string stringToSign)
        {

            RSA? privateKey = certificate.GetRSAPrivateKey();
            if (!certificate.HasPrivateKey || privateKey == null)
            {
                return string.Empty;
            }

            byte[] dataToSign = Encoding.UTF8.GetBytes(stringToSign);
            byte[] signature = privateKey.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signature);
        }

        public static bool VerifyString(string originalString, string signedString, string publicKeyJson)
        {
            try
            {
                RSAParametersJson? rsaParameters = JsonSerializer.Deserialize<RSAParametersJson>(publicKeyJson);

                if (rsaParameters == null)
                {
                    return false;
                }

                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportParameters(new RSAParameters
                    {
                        Modulus = Convert.FromBase64String(rsaParameters.Modulus),
                        Exponent = Convert.FromBase64String(rsaParameters.Exponent)
                    });

                    byte[] originalHashBytes = Encoding.UTF8.GetBytes(originalString);
                    byte[] signedHashBytes = Convert.FromBase64String(signedString);

                    return rsa.VerifyData(originalHashBytes, signedHashBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch
            {
                return false;
            }
        }

        public class RSAParametersJson
        {
            public string Modulus { get; set; } = string.Empty;
            public string Exponent { get; set; } = string.Empty;
        }

    }
}
