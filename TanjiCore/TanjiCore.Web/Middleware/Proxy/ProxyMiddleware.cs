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
using TanjiCore.Flazzy;
using TanjiCore.Intercept.Network;

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

        private void InterceptConnection()
        {
            string[] ports = Program.GameData.InfoPort.Split(',');
            if (ports.Length == 0 || !ushort.TryParse(ports[0], out ushort port) ||
                !HotelEndPoint.TryParse(Program.GameData.InfoHost, port, out HotelEndPoint endpoint))
            {
                throw new Exception("Failed to parse: " + Program.GameData.InfoPort);
            }
            Program.Connection.Intercept(endpoint);
        }
        private async Task<HttpContent> ReplaceContent(HttpResponseMessage responseMessage, string path)
        {
            if (path.StartsWith("/api/client/clienturl"))
            {
                string body = await responseMessage.Content.ReadAsStringAsync();
                body = body.Replace("https://www.habbo.com", "https://localhost:8081");

                return new ByteArrayContent(Encoding.UTF8.GetBytes(body));
            }
            else if (path.StartsWith("/client/hhus-") && string.IsNullOrWhiteSpace(Program.GameData.Source))
            {
                string body = await responseMessage.Content.ReadAsStringAsync();
                Program.GameData.Source = body;

                body = body.Replace(Program.GameData.InfoHost, "127.0.0.1")
                    .Replace("www.habbo.com", "localhost:8081")
                    .Replace("images.habbo.com\\/", "localhost:8081\\/imagesdomain\\/")
                    .Replace("images.habbo.com", "localhost:8081/imagesdomain");

                return new ByteArrayContent(Encoding.UTF8.GetBytes(body));
            }
            else if (path.EndsWith("Habbo.swf"))
            {
                Uri remoteUrl = responseMessage.RequestMessage.RequestUri;
                string clientPath = Path.GetFullPath($"Modified Clients/{remoteUrl.Host}/{remoteUrl.LocalPath}");

                // TODO: Where should we put these files?
                string clientDirectory = Path.GetDirectoryName(clientPath);
                Directory.CreateDirectory(clientDirectory);

                byte[] payload = await responseMessage.Content.ReadAsByteArrayAsync();
                Program.Game = new HGame(payload);
                Program.Game.Disassemble();

                // TODO: Program.GenerateMessageHashes();

                Program.Game.DisableHostChecks();
                Program.Game.InjectKeyShouter(4001);

                CompressionKind compression = CompressionKind.ZLIB;
#if DEBUG
                compression = CompressionKind.None;
#endif

                payload = Program.Game.ToArray(compression);
                File.WriteAllBytes(clientPath, payload); // This is so we can just intercept the HTTP(

                var interceptConnectionTask = Task.Factory.StartNew(
                    InterceptConnection, TaskCreationOptions.LongRunning);

                return new ByteArrayContent(payload);
            }
            return responseMessage.Content;
        }
    }
}