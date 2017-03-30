using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using TanjiCore.Intercept.Network;
using WebSocketManager;
using WebSocketManager.Common;

namespace TanjiCore.Web
{
    public class PacketHandler : WebSocketHandler
    {
        public PacketHandler(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
        {
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);

            /*var socketId = WebSocketConnectionManager.GetId(socket);

            var message = new Message()
            {
                MessageType = MessageType.Text,
                Data = $"{socketId} is now connected"
            };

            await SendMessage("outgoing", "test");
            await SendMessage("outgoing", "test");
            await SendMessage("outgoing", "test");
            await SendMessageToAllAsync(message);*/
        }

        public async Task SendMessage(string direction, string message)
        {
            await InvokeClientMethodToAllAsync("receiveMessage", direction, message);
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);

            await base.OnDisconnected(socket);

            var message = new Message()
            {
                MessageType = MessageType.Text,
                Data = $"{socketId} disconnected"
            };
            await SendMessageToAllAsync(message);
        }

        public void PacketOutgoing(object sender, DataInterceptedEventArgs e)
        {
            SendMessage("outgoing", $"Outgoing: [{e.Packet.Header}]: {e.Packet.ToString()}").Wait();
        }

        public void PacketIncoming(object sender, DataInterceptedEventArgs e)
        {
            SendMessage("incoming", $"Incoming: [{e.Packet.Header}]: {e.Packet.ToString()}").Wait();
        }
    }
}