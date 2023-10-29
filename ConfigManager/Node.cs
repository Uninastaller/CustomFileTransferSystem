using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ConfigManager
{
    public class Node
    {
        public string Id => $"{Address}:{Port}";
        public string Address { get; set; } = string.Empty;
        public int Port { get; set; }
        public string PublicKey { get; set; } = string.Empty;
    }
}
