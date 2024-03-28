using ConfigManager;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SslTcpSession.BlockChain
{
   public class Block
   {
      public Int32 Index { get; set; }
      public DateTime Timestamp { get; set; }
      public string FileHash { get; set; } = string.Empty; // For integrity check
      public Guid FileID { get; set; } // Unique identifier for the file
      public List<IpAndPortEndPoint>? FileLocations { get; set; }
      public TransactionType Transaction { get; set; }
      public string Hash { get; private set; } = string.Empty;
      public string PreviousHash { get; set; } = string.Empty;
      public Guid NodeId { get; set; }
      public double CreditChange { get; set; }
      public double NewCreditVaue { get; set; }
      public string SignedHash { get; private set; } = string.Empty;

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
         sb.Append(NewCreditVaue);

         using (SHA256 sha256 = SHA256.Create())
         {
            Hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
         }
      }

      public void SignHash(string subjectName = "ReplicaXY")
      {
         SignedHash = Certificats.SignString(Certificats.GetCertificate(subjectName, Certificats.CertificateType.Node), Hash);
      }

      public bool VerifyHash(string publicKeyAsString)
      {
         return Certificats.VerifyString(Hash, SignedHash, publicKeyAsString);
      }

      public string ToJson() => JsonSerializer.Serialize(this);
      public static Block? ToObjectFromJson(string jsonString) => JsonSerializer.Deserialize<Block>(jsonString);
   }
}