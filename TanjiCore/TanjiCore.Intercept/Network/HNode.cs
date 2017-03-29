using System;
using System.Net;
using System.Net.Sockets;

using TanjiCore.Intercept.Crypto;
using TanjiCore.Intercept.Network.Protocol;

namespace TanjiCore.Intercept.Network
{
    public class HNode : IDisposable
    {
        public bool IsConnected
        {
            get { return Client.Connected; }
        }

        public Socket Client { get; }
        public HotelEndPoint EndPoint { get; private set; }

        public RC4 Encrypter { get; set; }
        public bool IsEncrypting { get; set; }

        public RC4 Decrypter { get; set; }
        public bool IsDecrypting { get; set; }

        public HNode()
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        { }
        public HNode(Socket client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public void Connect(string host, int port)
        {
            Connect(HotelEndPoint.Parse(host, port));
        }
        public void Connect(IPEndPoint endpoint)
        {
            EndPoint = (endpoint as HotelEndPoint);
            if (EndPoint == null)
            {
                EndPoint = new HotelEndPoint(endpoint);
            }
            Client.Connect(endpoint);
        }
        public void Connect(IPAddress address, int port)
        {
            Connect(new HotelEndPoint(address, port));
        }
        public void Connect(IPAddress[] addresses, int port)
        {
            Connect(new HotelEndPoint(addresses[0], port));
        }

        public HMessage ReceivePacket()
        {
            byte[] lengthBlock = Receive(4);
            if (lengthBlock.Length != 4)
            {
                Disconnect();
                return null;
            }

            int totalBytesRead = 0;
            int nullBytesReadCount = 0;
            var body = new byte[BigEndian.ToInt32(lengthBlock, 0)];
            do
            {
                int bytesLeft = (body.Length - totalBytesRead);
                int bytesRead = Receive(body, totalBytesRead, bytesLeft);
                if (!IsConnected || (bytesRead == 0 && ++nullBytesReadCount >= 2))
                {
                    Disconnect();
                    return null;
                }

                nullBytesReadCount = 0;
                totalBytesRead += bytesRead;
            }
            while (totalBytesRead != body.Length);

            var data = new byte[4 + body.Length];
            Buffer.BlockCopy(lengthBlock, 0, data, 0, 4);
            Buffer.BlockCopy(body, 0, data, 4, body.Length);

            return new HMessage(data);
        }
        public int SendPacket(HMessage packet)
        {
            return Send(packet.ToBytes());
        }
        public int SendPacket(ushort id, params object[] values)
        {
            return Send(HMessage.Construct(id, values));
        }

        public int Send(byte[] buffer)
        {
            return Send(buffer, buffer.Length);
        }
        public int Send(byte[] buffer, int size)
        {
            return Send(buffer, 0, size);
        }
        public int Send(byte[] buffer, int offset, int size)
        {
            return Send(buffer, offset, size, SocketFlags.None);
        }

        public byte[] Receive(int size)
        {
            return ReceiveBuffer(size, SocketFlags.None);
        }
        public int Receive(byte[] buffer)
        {
            return Receive(buffer, buffer.Length);
        }
        public int Receive(byte[] buffer, int size)
        {
            return Receive(buffer, 0, size);
        }
        public int Receive(byte[] buffer, int offset, int size)
        {
            return Receive(buffer, offset, size, SocketFlags.None);
        }

        public byte[] Peek(int size)
        {
            return ReceiveBuffer(size, SocketFlags.Peek);
        }
        public int Peek(byte[] buffer)
        {
            return Peek(buffer, buffer.Length);
        }
        public int Peek(byte[] buffer, int size)
        {
            return Peek(buffer, 0, size);
        }
        public int Peek(byte[] buffer, int offset, int size)
        {
            return Receive(buffer, offset, size, SocketFlags.Peek);
        }

        protected byte[] ReceiveBuffer(int size, SocketFlags socketFlags)
        {
            var buffer = new byte[size];
            int read = Receive(buffer, 0, size, socketFlags);

            var trimmedBuffer = new byte[read];
            Buffer.BlockCopy(buffer, 0, trimmedBuffer, 0, read);

            return trimmedBuffer;
        }
        protected int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            if (!IsConnected)
            {
                return 0;
            }
            if (IsEncrypting && Encrypter != null)
            {
                buffer = Encrypter.Parse(buffer);
            }
            return Client.Send(buffer, offset, size, socketFlags);
        }
        protected int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            if (!IsConnected)
            {
                return 0;
            }

            int read = Client.Receive(buffer, offset, size, socketFlags);
            if (read > 0 && IsDecrypting && Decrypter != null)
            {
                Decrypter.RefParse(buffer, offset, read,
                    socketFlags.HasFlag(SocketFlags.Peek));
            }
            return read;
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                try
                {
                    Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false);
                    Client.Shutdown(SocketShutdown.Both);
                    Client.Dispose();
                }
                catch (SocketException) { }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (IsConnected)
                {
                    Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                    Client.Shutdown(SocketShutdown.Both);
                }
                Client.Dispose();
            }
        }

        public static HNode Accept(int port)
        {
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Any, port));
                listener.Listen(1);

                Socket client = listener.Accept();
                return new HNode(client);
            }
        }
        public static HNode ConnectNew(string host, int port)
        {
            return ConnectNew(HotelEndPoint.Parse(host, port));
        }
        public static HNode ConnectNew(IPEndPoint endpoint)
        {
            var node = new HNode();
            node.Connect(endpoint);

            return node;
        }
        public static HNode ConnectNew(IPAddress address, int port)
        {
            return ConnectNew(new HotelEndPoint(address, port));
        }
        public static HNode ConnectNew(IPAddress[] addresses, int port)
        {
            return ConnectNew(new HotelEndPoint(addresses[0], port));
        }
    }
}