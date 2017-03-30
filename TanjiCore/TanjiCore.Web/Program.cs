using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography.X509Certificates;
using TanjiCore.Intercept.Network;
using System.Threading.Tasks;
using TanjiCore.Intercept.Network.Protocol;
using System.Linq;
using TanjiCore.Intercept.Crypto;

namespace TanjiCore.Web
{
    public class Program
    {
        private static HConnection conn;

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

            Task.Factory.StartNew(() => {
                // TODO: intercept and modify policy file in-transit **REQUIRED**
                conn = new HConnection();
                conn.Intercept(HotelEndPoint.Parse("game-us.habbo.com", 38101));
                conn.DataOutgoing += Conn_DataOutgoing;
                conn.DataIncoming += Conn_DataIncoming;
            });

            host.Run();

            
        }

        private static void Conn_DataOutgoing(object sender, DataInterceptedEventArgs e)
        {
            if (e.Packet.Header == 4001)
            {
                string sharedKeyHex = e.Packet.ReadString();
                if (sharedKeyHex.Length % 2 != 0)
                    sharedKeyHex = ("0" + sharedKeyHex);

                byte[] sharedKey = Enumerable.Range(0, sharedKeyHex.Length / 2)
                    .Select(x => Convert.ToByte(sharedKeyHex.Substring(x * 2, 2), 16))
                    .ToArray();

                conn.Remote.Encrypter = new RC4(sharedKey);
                conn.Remote.IsEncrypting = true;
                e.IsBlocked = true;
            }

            if (e.Packet.Header == 157)
            {
                e.Packet.ReadInteger();
                string prod = e.Packet.ReadString().Replace("localhost:8081/imagesdomain", "images.habbo.com");
                string extvar = e.Packet.ReadString().Replace("localhost:8081", "www.habbo.com");
                e.Packet = new HMessage(157, 401, prod, extvar);
            }

            Console.WriteLine("Outgoing [{0}]: {1}", e.Packet.Header, e.Packet.ToString());
        }

        private static void Conn_DataIncoming(object sender, DataInterceptedEventArgs e)
        {
            Console.WriteLine("Incoming [{0}]: {1}", e.Packet.Header, e.Packet.ToString());
        }
    }
}
