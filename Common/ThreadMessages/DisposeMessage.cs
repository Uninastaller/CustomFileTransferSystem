using Common.Enum;
using Common.Model;
using System;
using System.Net;

namespace Common.ThreadMessages
{
    public class DisposeMessage : MsgBase<DisposeMessage>
    {
        public DisposeMessage(Guid sessionGuid, TypeOfSocket typeOfSocket, TypeOfSession typeOfSession, bool isPurposeFullfilled, string address, int port) : base(typeof(DisposeMessage))
        {
            SessionGuid = sessionGuid;
            TypeOfSocket = typeOfSocket;
            TypeOfSession = typeOfSession;
            IsPurposeFullfilled = isPurposeFullfilled;
            Address = address;
            Port = port;
        }

        public DisposeMessage() : base(typeof(DisposeMessage))
        {

        }

        public Guid SessionGuid { get; set; }
        public TypeOfSocket TypeOfSocket { get; set; }
        public TypeOfSession TypeOfSession { get; set; }
        public bool IsPurposeFullfilled { get; set; }
        public string Address { get; set; } = string.Empty;
        public int Port { get; set; }

    }
}
