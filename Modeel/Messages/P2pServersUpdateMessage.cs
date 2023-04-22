﻿using Modeel.Frq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Modeel.Messages
{
    internal class P2pServersUpdateMessage : MsgBase<P2pServersUpdateMessage>
    {
        public P2pServersUpdateMessage() : base(typeof(P2pServersUpdateMessage))
        {
        }

        public List<SslServerBussinesLogic> Servers { get; set; } = new List<SslServerBussinesLogic>();
    }
}
