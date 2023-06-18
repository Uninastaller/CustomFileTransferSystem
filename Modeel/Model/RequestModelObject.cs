using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public class RequestModelObject
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
