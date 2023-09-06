using Common.Enum;
using Common.Model;
using System;

namespace Common.ThreadMessages
{
    public class ServerSocketStateChangeMessage : MsgBase<ServerSocketStateChangeMessage>
    {
        public ServerSocketStateChangeMessage() : base(typeof(ServerSocketStateChangeMessage))
        {
        }

        //public TypeOfServerSocket TypeOfServerSocket { get; set; }
        public TypeOfSession TypeOfSession { get; set; }
        public ServerSocketState ServerSocketState { get; set; }
        //public Guid Id { get; set; }
    }
}
