using Modeel.Frq;
using System;
using System.Collections.Generic;

namespace Modeel.Messages
{
    internal class RefreshTablesMessage : MsgBase<RefreshTablesMessage>
    {
        public RefreshTablesMessage() : base(typeof(RefreshTablesMessage))
        {
        }

    }
}
