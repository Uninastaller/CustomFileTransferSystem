using Common.Interface;
using Common.Model;
using System.Collections.Generic;


namespace Common.ThreadMessages
{
    public class P2pServersUpdateMessage : MsgBase<P2pServersUpdateMessage>
    {
        public P2pServersUpdateMessage() : base(typeof(P2pServersUpdateMessage))
        {
        }

        public List<IUniversalServerSocket> Servers { get; set; } = new List<IUniversalServerSocket>();
    }
}
