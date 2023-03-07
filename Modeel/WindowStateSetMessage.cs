using Modeel.Frq;

namespace Modeel
{
    internal class WindowStateSetMessage : MsgBase<WindowStateSetMessage>
    {
        public WindowStateSetMessage() : base(typeof(WindowStateSetMessage))
        {
        }
    }
}
