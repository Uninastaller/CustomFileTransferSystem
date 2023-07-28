using Modeel.Frq;
using Modeel.Model.Enums;

namespace Modeel.Messages
{
    internal class SocketStateChangeMessage : MsgBase<SocketStateChangeMessage>
    {
        public SocketStateChangeMessage() : base(typeof(SocketStateChangeMessage))
        {
        }

        public TypeOfSession TypeOfSession { get; set; }
        public SocketState SocketState { get; set; }
    }
}
