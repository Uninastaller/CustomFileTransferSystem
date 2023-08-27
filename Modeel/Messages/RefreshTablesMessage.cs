using Common.Model;

namespace Modeel.Messages
{
    internal class RefreshTablesMessage : MsgBase<RefreshTablesMessage>
    {
        public RefreshTablesMessage() : base(typeof(RefreshTablesMessage))
        {
        }

    }
}
