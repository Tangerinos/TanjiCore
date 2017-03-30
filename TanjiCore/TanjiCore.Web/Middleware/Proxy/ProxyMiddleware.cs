// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;
using System.IO;
using TanjiCore.Intercept.Habbo;

namespace TanjiCore.Web.Middleware.Proxy
{
    public class ProxyMiddleware
    {
        private const int DefaultWebSocketBufferSize = 4096;

        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;
        private readonly ProxyOptions _options;

        private static readonly string[] NotForwardedWebSocketHeaders = new[] { "Connection", "Host", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version" };

        public ProxyMiddleware(RequestDelegate next, IOptions<ProxyOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options.Value;

            if (string.IsNullOrEmpty(_options.Host))
            {
                throw new ArgumentException("Options parameter must specify host.", nameof(options));
            }

            // Setting default Port and Scheme if not specified
            if (string.IsNullOrEmpty(_options.Port))
            {
                if (string.Equals(_options.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    _options.Port = "443";
                }
                else
                {
                    _options.Port = "80";
                }

            }

            if (string.IsNullOrEmpty(_options.Scheme))
            {
                _options.Scheme = "http";
            }

            _httpClient = new HttpClient(_options.BackChannelMessageHandler ?? new HttpClientHandler());
        }

        public Task Invoke(HttpContext context) => HandleHttpRequest(context);

        private async Task HandleHttpRequest(HttpContext context)
        {
            var requestMessage = new HttpRequestMessage();
            var requestMethod = context.Request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            foreach (var header in context.Request.Headers)
            {
                if (header.Key.Equals("Referer") || header.Key.Equals("Origin"))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, new string[] { header.Value[0].Replace("https://localhost:8081", "https://www.habbo.com") });
                }
                else
                {
                    if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                    {
                        requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            }

            requestMessage.Headers.Host = _options.Host + ":" + _options.Port;
            string path;

            if (_options.Strip)
                path = context.Request.Path.Value.Replace(_options.StripPath, "");
            else
                path = context.Request.Path;

            var uriString = $"{_options.Scheme}://{_options.Host}:{_options.Port}{context.Request.PathBase}{path}{context.Request.QueryString}";
            requestMessage.RequestUri = new Uri(uriString);
            requestMessage.Method = new HttpMethod(context.Request.Method);
            using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
            {
                responseMessage.Content = await ReplaceContent(responseMessage, path);
                context.Response.StatusCode = (int)responseMessage.StatusCode;

                foreach (var header in responseMessage.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in responseMessage.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
                context.Response.Headers.Remove("transfer-encoding");
                if (!context.Response.StatusCode.Equals(StatusCodes.Status204NoContent))
                    await responseMessage.Content.CopyToAsync(context.Response.Body);
            }
        }

        private async Task<HttpContent> ReplaceContent(HttpResponseMessage responseMessage, string path)
        {
            var content = await responseMessage.Content.ReadAsStringAsync();

            if (path.StartsWith("/api/client/clienturl"))
            {
                content = content.Replace("https://www.habbo.com", "https://localhost:8081");
                return new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            }
            else if (path.StartsWith("/client/hhus-"))
            {
                Program.GameData.Source = content;
                content = content.Replace(Program.GameData.InfoHost, "127.0.0.1");

                string flashClientUrl = Program.GameData["flash.client.url"];
                int gordonIndex = flashClientUrl.IndexOf("gordon");

                content = content.Replace(flashClientUrl, ("localhost:8081/imagesdomain/" + flashClientUrl.Substring(gordonIndex)));
                return new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            }
            else if (path.EndsWith("Habbo.swf"))
            {
                var port = ushort.Parse(Program.GameData.InfoPort.Split(',')[0]);

                Task interceptTask = Task.Factory.StartNew(
                    () => Program.Connection.Intercept(Program.GameData.InfoHost, port), TaskCreationOptions.LongRunning);

                return new ByteArrayContent(File.ReadAllBytes("asmd_Habbo.swf"));
            }
            return responseMessage.Content;
        }
    }
}