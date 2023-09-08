using Common.Model;
using System;
using System.Collections.Generic;

namespace Common.ThreadMessages
{
    public class ClientsStateChangeMessage : MsgBase<ClientsStateChangeMessage>
    {
        public ClientsStateChangeMessage() : base(typeof(ClientsStateChangeMessage))
        {
        }

        public Dictionary<Guid, ServerClientsModel> Clients { get; set; } = new Dictionary<Guid, ServerClientsModel>();

    }
}
