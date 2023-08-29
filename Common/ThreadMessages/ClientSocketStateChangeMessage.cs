using Common.Enum;
using Common.Model;

namespace Common.ThreadMessages
{
    public class ClientSocketStateChangeMessage : MsgBase<ClientSocketStateChangeMessage>
    {
        public ClientSocketStateChangeMessage() : base(typeof(ClientSocketStateChangeMessage))
        {
        }

        public TypeOfSession TypeOfSession { get; set; }
        public ClientSocketState ClientSocketState { get; set; }
    }
}
