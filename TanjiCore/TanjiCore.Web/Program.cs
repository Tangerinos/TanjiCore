using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography.X509Certificates;
using TanjiCore.Intercept.Network;
using System.Threading.Tasks;

namespace TanjiCore.Web
{
    public class Program
    {
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
                var conn = new HConnection();
                conn.Intercept(HotelEndPoint.Parse("game-us.habbo.com", 38101));
                conn.DataOutgoing += Conn_DataOutgoing;
                conn.DataIncoming += Conn_DataIncoming;
            });

            host.Run();

            
        }

        private static void Conn_DataOutgoing(object sender, DataInterceptedEventArgs e)
        {
            Console.WriteLine(e.Packet.ToString());
        }

        private static void Conn_DataIncoming(object sender, DataInterceptedEventArgs e)
        {
            Console.WriteLine(e.Packet.ToString());
        }
    }
}
