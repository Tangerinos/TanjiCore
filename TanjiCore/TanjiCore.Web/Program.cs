using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography.X509Certificates;

namespace TanjiCore.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var class1 = new Intercept.Class1();

            var cert = new X509Certificate2("Kestrel.pfx", "password");

            var host = new WebHostBuilder()
                .UseKestrel(conf =>
                {
                    conf.NoDelay = true;
                    // this doesn't really need to be secure
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
