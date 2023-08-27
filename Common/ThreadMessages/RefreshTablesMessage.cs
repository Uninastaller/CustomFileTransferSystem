using Common.Model;

namespace Common.ThreadMessages
{
    public class RefreshTablesMessage : MsgBase<RefreshTablesMessage>
    {
        public RefreshTablesMessage() : base(typeof(RefreshTablesMessage))
        {
        }

    }
}
