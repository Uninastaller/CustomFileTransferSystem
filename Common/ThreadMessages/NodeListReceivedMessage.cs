using Common.Model;
using ConfigManager;
using System;
using System.Collections.Generic;

namespace Common.ThreadMessages
{
    public class NodeListReceivedMessage : MsgBase<NodeListReceivedMessage>
    {
        public NodeListReceivedMessage(Dictionary<Guid, Node> nodeDict) : base(typeof(NodeListReceivedMessage))
        {
            NodeDict = nodeDict;
        }

        public Dictionary<Guid, Node> NodeDict { get; set; }
    }
}
