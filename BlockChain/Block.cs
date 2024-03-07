using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BlockChain
{
    public class Block
    {
        public Int32 Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string FileHash { get; set; } = string.Empty; // For integrity check
        public Guid FileID { get; set; } // Unique identifier for the file
        public List<EndPoint> FileLocations { get; set; }  = new List<EndPoint>();
        public TransactionType Transaction { get; set; }
        public string Hash { get; set; } = string.Empty;
        public string PreviousHash { get; set; } = string.Empty;

        public string ComputeHash()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes($"{Index}{Timestamp}{FileHash}{FileID}{string.Join(",", FileLocations)}{Transaction}{PreviousHash}")));
            }
        }


    }
}