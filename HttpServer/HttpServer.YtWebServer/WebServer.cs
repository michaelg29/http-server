using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HttpServer.Logging;

namespace HttpServer.YtWebServer
{
    public class WebServer : Main.HttpServer
    {
        // any type we can return with obj.ToString()
        private static readonly ICollection<Type> primitiveTypes = new HashSet<Type>
        {
            typeof(decimal),
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid)
        };

        public RouteTree RouteTree { get; private set; }

        public WebServer(string hostUrl, string hostDir, ILogger logger)
            : base(hostUrl, hostDir, logger)
        {
            RouteTree = new RouteTree();
        }

        protected override Task Startup()
        {
            return null;
        }

        protected override async Task<string> GetCommand()
        {
            return await Task.FromResult(Console.ReadLine());
        }

        protected override async Task ProcessRequest()
        {
            // define variables
            (bool, object, Type) res;
            object retObj = null;
            Type retType = null;

            // 1) parse body
            var bodyStream = ctx.Request.InputStream;
            string bodyStr = null;
            byte[] bodyBuffer = null;
            IDictionary<string, string> formParams = null;

            if (bodyStream != null)
            {
                if (!string.IsNullOrEmpty(ctx.Request.ContentType) &&
                    Mime.TextMimeTypes.Any(
                        s => Regex.Match(ctx.Request.ContentType, s).Success))
                {
                    // read as text
                    var body = new StringBuilder();
                    byte[] buffer = new byte[1024];
                    int noBytes;
                    while ((noBytes = bodyStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        body = body.Append((Request.ContentEncoding ?? Encoding.UTF8).GetString(buffer, 0, noBytes));
                    }

                    if (ctx.Request.ContentType == "application/x-www-form-urlencoded")
                    {
                        formParams = body.ToString().ParseAsQuery();
                    }
                    else
                    {
                        bodyStr = body.ToString();
                    }
                }
                else
                {
                    // read as buffer
                    List<byte> bytes = new List<byte>();
                    byte[] buffer = new byte[1024];
                    int noBytes;
                    while ((noBytes = bodyStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bytes.AddRange(buffer);

                        // truncate garbage buffer data
                        int diff = buffer.Length - noBytes;
                        if (diff > 0)
                        {
                            bytes.RemoveRange(bytes.Count - diff, diff);
                        }
                    }
                    bodyBuffer = bytes.ToArray();
                }
            }

            // 2) find and call the function for the endpoint
            res = await RouteTree.TryNavigate(new HttpMethod(ctx.Request.HttpMethod),
                ctx.Request.RawUrl, bodyStr, bodyBuffer, formParams);
            if (res.Item1)
            {
                retObj = res.Item2;
                retType = res.Item3;
            }
            else
            {
                ResponseCode = HttpStatusCode.NotFound;
            }

            // 3) send the return object
            if (!(retObj == null || retType == null))
            {
                if (retType.IsPrimitive || primitiveTypes.Contains(retType))
                {
                    await SendStringAsync(retObj.ToString(), ctx.Response.ContentType ?? "text");
                }
                else if (retType == typeof(HttpResponseMessage))
                {
                    await SendResponseAsync(retObj as HttpResponseMessage);
                }
                else
                {
                    await SendStringAsync(JsonConvert.SerializeObject(retObj, retType, null), "text/json");
                }
            }
            else if (res.Item1)
            {
                // successful call, no return
                ResponseCode = HttpStatusCode.NoContent;
            }
        }
    }
}
