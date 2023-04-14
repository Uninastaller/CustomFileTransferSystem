using Modeel.Frq;

namespace Modeel.Messages
{
    internal class WindowStateSetMessage : MsgBase<WindowStateSetMessage>
    {
        public WindowStateSetMessage() : base(typeof(WindowStateSetMessage))
        {
        }
    }
}
