namespace Common
{
    public class MessageItem
    {
        public MessageItem(string msg, MessageType msgType)
        {
            Message = msg;
            MessageType = msgType;
        }

        public string Message { get; set; }
        public MessageType MessageType { get; set; }
    }
}
