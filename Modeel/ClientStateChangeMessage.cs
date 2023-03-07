using Modeel.Frq;
using System;

namespace Modeel
{
    internal class ClientStateChangeMessage : MsgBase<ClientStateChangeMessage>
    {
        public ClientStateChangeMessage() : base(typeof(ClientStateChangeMessage))
        {
        }
        public string Client { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
        public ClientState State { get; set; }
    }
}
