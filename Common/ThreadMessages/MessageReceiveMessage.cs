using Common.Model;

namespace Common.ThreadMessages
{
    public class MessageReceiveMessage : MsgBase<MessageReceiveMessage>
    {
        public MessageReceiveMessage() : base(typeof(MessageReceiveMessage))
        {
        }

        public string Message { get; set; }
    }
}
