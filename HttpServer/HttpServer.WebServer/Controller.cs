using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer
{
    public abstract class Controller
    {
        internal IHttpServer _server { get; set; }

        protected void Shutdown()
        {
            _server.Shutdown();
        }

        protected HttpListenerRequest Request
        {
            get => _server.Request;
        }

        public void SetResponse(HttpStatusCode responseCode = 0,
            string contentType = null,
            Encoding encoding = null)
        {
            if (responseCode != 0)
            {
                _server.ResponseCode = responseCode;
            }
            if (!string.IsNullOrEmpty(contentType))
            {
                _server.ContentType = contentType;
            }
            if (encoding != null)
            {
                _server.Encoding = encoding;
            }
        }
    }
}
