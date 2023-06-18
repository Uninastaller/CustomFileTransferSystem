using Modeel.Frq;

namespace Modeel.Messages
{
    internal class MessageReceiveMessage : MsgBase<MessageReceiveMessage>
    {
        public MessageReceiveMessage() : base(typeof(MessageReceiveMessage))
        {
        }

        public string Message { get; set; }
    }
}
