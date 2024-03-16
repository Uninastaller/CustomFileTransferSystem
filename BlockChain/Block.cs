using ConfigManager;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BlockChain
{
   public class Block
   {
      public Int32 Index { get; set; }
      public DateTime Timestamp { get; set; }
      public string FileHash { get; set; } = string.Empty; // For integrity check
      public Guid FileID { get; set; } // Unique identifier for the file
      public List<EndPoint> FileLocations { get; set; } = new List<EndPoint>();
      public TransactionType Transaction { get; set; }
      public string Hash { get; private set; } = string.Empty;
      public string PreviousHash { get; set; } = string.Empty;
      public Guid NodeId { get; set; }
      public double CreditChange { get; set; }
      public double NewCreditVaue { get; set; }
      public string SignedHash {  get; private set; } = string.Empty;

      public void ComputeHash()
      {
         using (SHA256 sha256 = SHA256.Create())
         {
            Hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes($"{Index}{Timestamp}{FileHash}{FileID}{string.Join(",", FileLocations)}{Transaction}{PreviousHash}{NodeId}{CreditChange}{NewCreditVaue}")));
         }
      }

      public void SignHash(string subjectName = "NodeXY")
      {
         SignedHash = Certificats.SignString(Certificats.GetCertificate(subjectName, Certificats.CertificateType.Node), Hash);
      }

      public bool VerifyHash(string publicKeyAsString)
      {
         return Certificats.VerifyString(Hash, SignedHash, publicKeyAsString);
      }

   }
}