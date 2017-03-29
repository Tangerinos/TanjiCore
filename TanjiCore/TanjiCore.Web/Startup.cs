using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TanjiCore.Web
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        private static bool IsHabboCms(HttpContext httpContext)
        {
            return httpContext.Request.Path.Value.StartsWith(@"/", StringComparison.OrdinalIgnoreCase) && !httpContext.Request.Path.Value.Contains("/imagesdomain");
        }

        private static bool IsGameData(HttpContext httpContext) =>
            httpContext.Request.Path.Value.StartsWith(@"/imagesdomain/", StringComparison.OrdinalIgnoreCase);
    }
}
