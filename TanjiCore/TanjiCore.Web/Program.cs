using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using TanjiCore.Intercept.Crypto;
using TanjiCore.Intercept.Habbo;
using TanjiCore.Intercept.Network;
using System.Threading.Tasks;
using WebSocketManager;

namespace TanjiCore.Web
{
    public class Program
    {
        public static HGame Game { get; set; }
        public static HGameData GameData { get; }
        public static HConnection Connection { get; }
        public static PacketHandler Handler { get; private set; }

        static Program()
        {
            GameData = new HGameData();

            Connection = new HConnection();
            Connection.DataOutgoing += DataOutgoing;
            Connection.DataIncoming += DataIncoming;
        }

        public static void Main(string[] args)
        {
            var cert = new X509Certificate2("Kestrel.pfx", "password");
            var host = new WebHostBuilder()
                .UseKestrel(conf =>
                {
                    conf.NoDelay = true;
                    conf.UseHttps(cert);
                })
                .UseUrls("https://localhost:8081")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            Handler = host.Services.GetService<PacketHandler>();
            
            Connection.DataOutgoing += Handler.PacketOutgoing;
            Connection.DataIncoming += Handler.PacketIncoming;

            host.Run();
        }

        private static void DataOutgoing(object sender, DataInterceptedEventArgs e)
        {
            if (e.Packet.Header == 4001)
            {
                string sharedKeyHex = e.Packet.ReadString();

                if (sharedKeyHex.Length % 2 != 0)
                    sharedKeyHex = ("0" + sharedKeyHex);

                byte[] sharedKey = Enumerable.Range(0, sharedKeyHex.Length / 2)
                    .Select(x => Convert.ToByte(sharedKeyHex.Substring(x * 2, 2), 16))
                    .ToArray();

                Connection.Remote.Encrypter = new RC4(sharedKey);
                Connection.Remote.IsEncrypting = true;
                e.IsBlocked = true;
            }

            Console.WriteLine("Outgoing[{0}]: {1}\r\n----------", e.Packet.Header, e.Packet);
        }
        private static void DataIncoming(object sender, DataInterceptedEventArgs e)
        {
            Console.WriteLine("Incoming[{0}]: {1}\r\n----------", e.Packet.Header, e.Packet);
        }
    }
}