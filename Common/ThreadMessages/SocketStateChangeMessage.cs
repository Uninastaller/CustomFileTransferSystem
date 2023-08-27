using Common.Enum;
using Common.Model;

namespace Common.ThreadMessages
{
    public class SocketStateChangeMessage : MsgBase<SocketStateChangeMessage>
    {
        public SocketStateChangeMessage() : base(typeof(SocketStateChangeMessage))
        {
        }

        public TypeOfSession TypeOfSession { get; set; }
        public SocketState SocketState { get; set; }
    }
}
