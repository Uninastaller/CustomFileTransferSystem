using Common.Model;
using Modeel.Frq;
using Modeel.Model.Enums;
using System;

namespace Modeel.Messages
{
    public class DisposeMessage : MsgBase<DisposeMessage>
    {
        public DisposeMessage(Guid sessionGuid, TypeOfSocket typeOfSocket) : base(typeof(DisposeMessage))
        {
            SessionGuid = sessionGuid;
            TypeOfSocket = typeOfSocket;
        }

        public DisposeMessage() : base(typeof(DisposeMessage))
        {
            
        }

        public Guid SessionGuid { get; set; }
        public TypeOfSocket TypeOfSocket { get; set; }

    }
}
