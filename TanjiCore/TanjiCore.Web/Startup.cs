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
            if (env.IsDevelopment())
            {
                loggerFactory.AddConsole();
                app.UseDeveloperExceptionPage();
            }

            app.MapWhen(IsUi, builder => builder.UseFileServer());

            app.UseWebSockets();
            app.MapWebSocketManager("/packet", serviceProvider.GetService<PacketHandler>());

            app.MapWhen(IsGameData, builder => builder.RunProxy(new ProxyOptions
            {
                Scheme = "https",
                Host = "images.habbo.com",
                Port = "443",
                Strip = true,
                StripPath = "/imagesdomain"
            }));

            app.MapWhen(IsHabboCms, builder => builder.RunProxy(new ProxyOptions
            {
                Scheme = "https",
                Host = "www.habbo.com",
                Port = "443",
                Strip = false,
                StripPath = ""
            }));
        }

        private static bool IsUi(HttpContext httpContext) =>
            httpContext.Request.Path.Value.StartsWith("/ui", StringComparison.OrdinalIgnoreCase);

        private static bool IsPacket(HttpContext httpContext) =>
            httpContext.Request.Path.Value.StartsWith("/packet", StringComparison.OrdinalIgnoreCase);

        private static bool IsGameData(HttpContext httpContext) =>
            httpContext.Request.Path.Value.StartsWith("/imagesdomain", StringComparison.OrdinalIgnoreCase);

        private static bool IsHabboCms(HttpContext httpContext) =>
            httpContext.Request.Path.Value.StartsWith("/", StringComparison.OrdinalIgnoreCase);
    }
}
