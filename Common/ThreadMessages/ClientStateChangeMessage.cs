using Common.Model;
using System;
using System.Collections.Generic;

namespace Common.ThreadMessages
{
    public class ClientStateChangeMessage : MsgBase<ClientStateChangeMessage>
    {
        public ClientStateChangeMessage() : base(typeof(ClientStateChangeMessage))
        {
        }

        public Dictionary<Guid, string> Clients { get; set; } = new Dictionary<Guid, string>();

    }
}
