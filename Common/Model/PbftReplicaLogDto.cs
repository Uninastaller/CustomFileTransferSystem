using Common.Enum;
using Common.ThreadMessages;
using System;

namespace Common.Model
{
    public class PbftReplicaLogDto : MsgBase<PbftReplicaLogDto>
    {
        public PbftReplicaLogDto(SocketMessageFlag messageType, MessageDirection messageDirection, string synchronizationHash,
            string hashOfRequest, string receiverId, string senderId, DateTime time) : base(typeof(PbftReplicaLogDto))
        {
            MessageType = messageType;
            MessageDirection = messageDirection;
            SynchronizationHash = synchronizationHash;
            HashOfRequest = hashOfRequest;
            ReceiverId = receiverId;
            SenderId = senderId;
            Time = time;
        }

        public SocketMessageFlag MessageType { get; set; }
        public MessageDirection MessageDirection { get; set; }
        public string SynchronizationHash { get; set; } = string.Empty;
        public string HashOfRequest { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public DateTime Time { get; set; } = DateTime.MinValue;
        public string TimeAsString => Time.ToString("HH:mm:ss:fff");
    }
}
