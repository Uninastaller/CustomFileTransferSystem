using Common.Model;
using Modeel.Frq;
using System;
using System.Collections.Generic;

namespace Modeel.Messages
{
    internal class ClientStateChangeMessage : MsgBase<ClientStateChangeMessage>
    {
        public ClientStateChangeMessage() : base(typeof(ClientStateChangeMessage))
        {
        }

        public Dictionary<Guid, string> Clients { get; set; } = new Dictionary<Guid, string>();

    }
}
