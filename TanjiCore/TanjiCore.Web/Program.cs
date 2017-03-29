using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography.X509Certificates;
using TanjiCore.Intercept.Network;

namespace TanjiCore.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // TODO: intercept and modify policy file in-transit **REQUIRED**
            var conn = new HConnection();
            conn.Intercept(HotelEndPoint.Parse("game-us.habbo.com", 38101));

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

            host.Run();
        }
    }
}
