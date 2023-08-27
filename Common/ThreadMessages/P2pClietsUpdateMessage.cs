using Common.Interface;
using Common.Model;
using System.Collections.Generic;

namespace Common.ThreadMessages
{
    public class P2pClietsUpdateMessage : MsgBase<P2pClietsUpdateMessage>
    {
        public P2pClietsUpdateMessage() : base(typeof(P2pClietsUpdateMessage))
        {
        }

        public List<IUniversalClientSocket> Clients { get; set; } = new List<IUniversalClientSocket>();
    }
}
