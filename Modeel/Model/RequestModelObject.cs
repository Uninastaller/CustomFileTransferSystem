using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public class RequestModelObject
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public List<BaseClient> Clients { get; set; } = new List<BaseClient>();
    }

    public class BaseClient
    {
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
