using Common.Enum;
using Common.Model;
using System;
using System.Net;

namespace SslTcpSession.BlockChain.ThreadMessages
{
    public class PbftPrePrepareMessageReceivedMessage : MsgBase<PbftPrePrepareMessageReceivedMessage>
    {
        public PbftPrePrepareMessageReceivedMessage(Block requestedBlock) : base(typeof(PbftPrePrepareMessageReceivedMessage))
        {
            RequestedBlock = requestedBlock;
        }

        public Block RequestedBlock {  get; set; }

    }
}
