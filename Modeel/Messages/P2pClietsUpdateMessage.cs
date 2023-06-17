using Modeel.Frq;
using Modeel.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Modeel.Messages
{
    internal class P2pClietsUpdateMessage : MsgBase<P2pClietsUpdateMessage>
    {
        public P2pClietsUpdateMessage() : base(typeof(P2pClietsUpdateMessage))
        {
        }

        public List<IUniversalClientSocket> Clients { get; set; } = new List<IUniversalClientSocket>();
    }
}
