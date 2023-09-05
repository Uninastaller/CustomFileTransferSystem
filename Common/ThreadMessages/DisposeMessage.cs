using Common.Enum;
using Common.Model;
using System;

namespace Common.ThreadMessages
{
    public class DisposeMessage : MsgBase<DisposeMessage>
    {
        public DisposeMessage(Guid sessionGuid, TypeOfSocket typeOfSocket, TypeOfSession typeOfSession) : base(typeof(DisposeMessage))
        {
            SessionGuid = sessionGuid;
            TypeOfSocket = typeOfSocket;
            TypeOfSession = typeOfSession;
        }

        public DisposeMessage() : base(typeof(DisposeMessage))
        {

        }

        public Guid SessionGuid { get; set; }
        public TypeOfSocket TypeOfSocket { get; set; }
        public TypeOfSession TypeOfSession { get; set; }

    }
}
