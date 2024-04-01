using ConfigManager;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SslTcpSession.BlockChain
{
    public class Block
    {
        public Int32 Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string FileHash { get; set; } = string.Empty; // For integrity check
        public UInt64 FileSize { get; set; }

        public Guid FileID { get; set; }   // Unique identifier for the file

        [JsonIgnore]
        public string FileIDAsString    // FOR DB
        {
            get => FileID.ToString();
            set => FileID = Guid.Parse(value);
        }

        public List<IpAndPortEndPoint>? FileLocations { get; set; }
        public TransactionType Transaction { get; set; }
        public string Hash { get; set; } = string.Empty;
        public string PreviousHash { get; set; } = string.Empty;

        public Guid NodeId {  get; set; }

        [JsonIgnore]
        public string NodeIdAsString    // FOR DB
        {
            get => NodeId.ToString();
            set => NodeId = Guid.Parse(value);
        }

        public double CreditChange { get; set; }
        public double NewCreditValue { get; set; }
        public string SignedHash { get; set; } = string.Empty;

        // FOR datagrid
        [JsonIgnore]
        public string TimeInCustomFormat => Timestamp.ToString("dd.MM.yyyy HH:mm:ss:fff");
        [JsonIgnore]
        public string FileLocationsInJsonFormat
        {
            get => JsonSerializer.Serialize(FileLocations);
            set => FileLocations = JsonSerializer.Deserialize<List<IpAndPortEndPoint>>(value);
        }

        public void ComputeHash()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Index);
            sb.Append(Timestamp.ToString("o")); // Universal time format
            sb.Append(FileHash);
            sb.Append(FileID);

            if (FileLocations != null)
            {
                foreach (IpAndPortEndPoint location in FileLocations)
                {
                    sb.Append(location.ToString());
                }
            }

            sb.Append(Transaction);
            sb.Append(PreviousHash);
            sb.Append(NodeId);
            sb.Append(CreditChange);
            sb.Append(NewCreditValue);

            using (SHA256 sha256 = SHA256.Create())
            {
                Hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
            }
        }

        public void SignHash(string certificateSubjectName = "ReplicaXY")
        {
            SignedHash = Certificats.SignString(Certificats.GetCertificate(certificateSubjectName, Certificats.CertificateType.Node), Hash);
        }

        public string SignAndReturnHash(string certificateSubjectName = "ReplicaXY")
        {
            return Certificats.SignString(Certificats.GetCertificate(certificateSubjectName, Certificats.CertificateType.Node), Hash);
        }

        public bool VerifyHash(string publicKeyAsString)
        {
            return Certificats.VerifyString(Hash, SignedHash, publicKeyAsString);
        }

        public string ToJson() => JsonSerializer.Serialize(this);
        public static Block? ToObjectFromJson(string jsonString) => JsonSerializer.Deserialize<Block>(jsonString);

    }
}