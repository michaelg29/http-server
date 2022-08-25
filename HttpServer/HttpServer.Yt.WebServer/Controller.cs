using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Yt.WebServer
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class ControllerEndpoint : Attribute
    {
        public string Route { get; set; }
        public HttpMethod Method { get; set; }

        public ControllerEndpoint(string method, string route)
        {
            Method = new HttpMethod(method);
            Route = route;
        }

        internal ControllerEndpoint(HttpMethod method, string route)
        {
            Method = method;
            Route = route;
        }
    }

    public class HttpGet : ControllerEndpoint
    {
        public HttpGet(string route)
            : base(HttpMethod.Get, route)
        {

        }
    }

    public class HttpPost : ControllerEndpoint
    {
        public HttpPost(string route)
            : base(HttpMethod.Post, route)
        {

        }
    }

    public abstract class Controller
    {
        internal WebServer _server { get; set; }

        protected void Shutdown()
        {
            _server.Shutdown();
        }

        protected HttpListenerRequest Request
        {
            get => _server.Request;
        }
    }
}
