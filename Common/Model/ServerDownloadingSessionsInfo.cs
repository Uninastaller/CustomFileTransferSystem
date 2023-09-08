using Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Model
{
    public class ServerDownloadingSessionsInfo
    {
        public Guid Id { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public string FileNameOfAcceptedfileRequest { get; set; } = string.Empty;
        public SessionState SessionState { get; set; }
    }
}
