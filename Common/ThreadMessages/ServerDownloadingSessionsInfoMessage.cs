using Common.Model;
using System.Collections.Generic;

namespace Common.ThreadMessages
{
    public class ServerDownloadingSessionsInfoMessage : MsgBase<ServerDownloadingSessionsInfoMessage>
    {
        public ServerDownloadingSessionsInfoMessage(List<ServerDownloadingSessionsInfo>? serverDownloadingSessionsInfo) : base(typeof(ServerDownloadingSessionsInfoMessage))
        {
            ServerDownloadingSessionsInfo = serverDownloadingSessionsInfo;
        }

        public List<ServerDownloadingSessionsInfo>? ServerDownloadingSessionsInfo { get; set; }
    }
}
