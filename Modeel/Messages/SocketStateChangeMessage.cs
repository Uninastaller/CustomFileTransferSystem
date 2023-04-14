using Modeel.Frq;

namespace Modeel.Messages
{
    internal class SocketStateChangeMessage : MsgBase<SocketStateChangeMessage>
    {
        public SocketStateChangeMessage() : base(typeof(SocketStateChangeMessage))
        {
        }

        public SocketState SocketState { get; set; }
    }
}
