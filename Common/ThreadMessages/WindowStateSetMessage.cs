using Common.Model;

namespace Common.ThreadMessages
{
    public class WindowStateSetMessage : MsgBase<WindowStateSetMessage>
    {
        public WindowStateSetMessage() : base(typeof(WindowStateSetMessage))
        {
        }
    }
}
