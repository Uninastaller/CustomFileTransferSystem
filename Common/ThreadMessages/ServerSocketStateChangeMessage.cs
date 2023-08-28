using Common.Enum;
using Common.Model;

namespace Common.ThreadMessages
{
    public class ServerSocketStateChangeMessage : MsgBase<ServerSocketStateChangeMessage>
    {
        public ServerSocketStateChangeMessage() : base(typeof(ServerSocketStateChangeMessage))
        {
        }

        //public TypeOfSession TypeOfSession { get; set; }
        public ServerSocketState ServerSocketState { get; set; }
    }
}
