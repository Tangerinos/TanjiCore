using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebSocketManager;

namespace TanjiCore.Web
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketManager();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.MapWhen(IsHabboCms, builder => builder.RunProxy(new ProxyOptions
            {
                Scheme = "https",
                Host = "www.habbo.com",
                Port = "443",
                Strip = false,
                StripPath = ""
            }));

            app.MapWhen(IsGameData, builder => builder.RunProxy(new ProxyOptions
            {
                Scheme = "https",
                Host = "images.habbo.com",
                Port = "443",
                Strip = true,
                StripPath = "/imagesdomain"
            }));

            app.UseWebSockets();
            app.MapWebSocketManager("/packet", serviceProvider.GetService<PacketHandler>());

            app.UseStaticFiles();
        }

        private static bool IsHabboCms(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.Value;

            return path.StartsWith(@"/", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/imagesdomain", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/ui", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/packet", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGameData(HttpContext httpContext) =>
            httpContext.Request.Path.Value.StartsWith(@"/imagesdomain/", StringComparison.OrdinalIgnoreCase);
    }
}
