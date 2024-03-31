using Common.Enum;
using Common.ThreadMessages;
using System;
using System.Globalization;

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

        public PbftReplicaLogDto(SocketMessageFlag messageType, MessageDirection messageDirection, string synchronizationHash,
            string hashOfRequest, string receiverId, string senderId, DateTime time, string message) : base(typeof(PbftReplicaLogDto))
        {
            MessageType = messageType;
            MessageDirection = messageDirection;
            SynchronizationHash = synchronizationHash;
            HashOfRequest = hashOfRequest;
            ReceiverId = receiverId;
            SenderId = senderId;
            Time = time;
            Message = message;
        }

        public PbftReplicaLogDto() : base(typeof(PbftReplicaLogDto))
        {
            
        }

        public int Id { get; set; }
        public SocketMessageFlag MessageType { get; set; }
        public MessageDirection MessageDirection { get; set; }  
        public string SynchronizationHash { get; set; } = string.Empty;
        public string HashOfRequest { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public DateTime Time { get; set; } = DateTime.MinValue;
        public string? Message { get; set; }

        public string TimeInCustomFormat => Time.ToString("dd.MM.yyyy HH:mm:ss:fff");

    }
}
