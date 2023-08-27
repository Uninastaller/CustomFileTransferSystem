using Common.Enum;
using System.Collections.Generic;

namespace Common.Model
{
    public class RequestModelObject
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public List<BaseClient> Clients { get; set; } = new List<BaseClient>();
    }

    public class BaseClient
    {
        public bool UseThisClient { get; set; } = true;
        public TypeOfClientSocket TypeOfSocket { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
