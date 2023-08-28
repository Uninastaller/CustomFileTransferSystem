using Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Model
{
    public class ServerClientsModel
    {
        public Guid SessionGuid { get; set; }
        public string RemoteEndpoint { get; set; }
        public ServerSessionState ServerSessionState { get; set; }
    }
}
