namespace WebSocketManager.Common
{
    public enum MessageType
    {
        Text,
        IncomingPacket,
        OutgoingPacket,
        ClientMethodInvocation,
        ConnectionEvent
    }

    public class Message
    {
        public MessageType MessageType { get; set; }
        public string Data { get; set; }
    }
}