﻿using Modeel.Frq;
using Modeel.Model;

namespace Modeel.Messages
{
    internal class SocketStateChangeMessage : MsgBase<SocketStateChangeMessage>
    {
        public SocketStateChangeMessage() : base(typeof(SocketStateChangeMessage))
        {
        }

        public bool SessionWithCentralServer { get; set; }
        public SocketState SocketState { get; set; }
    }
}
