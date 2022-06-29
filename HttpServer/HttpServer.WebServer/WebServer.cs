using HttpServer.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer
{
    public class WebServer : Main.HttpServer
    {
        private static readonly IList<Type> primitiveTypes = new List<Type>
        {
            typeof(decimal),
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid)
        };

        public RouteTree RouteTree { get; private set; }

        /// <inheritdoc />
        public WebServer(string hostUrl = null, string hostDir = null, ILogger logger = null)
            : base(hostUrl, hostDir, logger)
        {
            RouteTree = new RouteTree();
        }

        /// <summary>
        /// Process current request
        /// </summary>
        protected override async Task ProcessRequest()
        {
            try
            {
                var bodyStream = ctx.Request.InputStream;
                var body = new StringBuilder();
                byte[] buffer = new byte[1024];
                int noBytes;
                while ((noBytes = bodyStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    body = body.Append(Encoding.UTF8.GetString(buffer, 0, noBytes));
                }

                if (RouteTree.TryNavigate(
                    new HttpMethod(ctx.Request.HttpMethod), ctx.Request.RawUrl,
                    out object ret, out Type type,
                    body.ToString()))
                {
                    if (ret != null)
                    {
                        // send returned content
                        ResponseCode = HttpStatusCode.OK;
                        if (type.IsPrimitive || primitiveTypes.Contains(type))
                        {
                            await SendStringAsync(ret.ToString());
                        }
                        else
                        {
                            // serialize complex object to JSON
                            await SendStringAsync(JsonConvert.SerializeObject(ret, type, null), "text/json");
                        }
                    }
                }
                else
                {
                    // send 404 file
                    ResponseCode = HttpStatusCode.NotFound;
                    await _SendFileAsync(GetTemplatePath(notFoundPath));
                }
            }
            catch (HttpListenerException e)
            {
                // HTTP Exception, cannot send another file
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
            }
            catch (Exception e)
            {
                // General exception, send to user
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
                await ReadFormattedFileToResponseAsync(GetTemplatePath(errorPath),
                    e.GetType().ToString(), e.Message);
            }
        }

        /// <inheritdoc />
        protected override async Task<string> GetCommand()
        {
            return await Task.FromResult(Console.ReadLine());
        }
    }
}
