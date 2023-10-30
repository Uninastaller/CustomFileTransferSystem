using Common.Model;
using ConfigManager;
using System.Collections.Generic;

namespace Common.ThreadMessages
{
    public class NodeListReceivedMessage : MsgBase<NodeListReceivedMessage>
    {
        public NodeListReceivedMessage(Dictionary<string, Node> nodeDict) : base(typeof(NodeListReceivedMessage))
        {
            NodeDict = nodeDict;
        }

        public Dictionary<string, Node> NodeDict { get; set; }
    }
}
