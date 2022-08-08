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
        internal WebServer _server { get; set; }

        protected void Shutdown()
        {
            _server.Shutdown();
        }

        protected HttpListenerRequest Request
        {
            get => _server.Request;
        }

        /// <summary>
        /// Set values in the HTTP response
        /// </summary>
        /// <param name="responseCode">Response code</param>
        /// <param name="contentType">Content type</param>
        /// <param name="encoding">Content encoding</param>
        protected void SetResponse(HttpStatusCode responseCode = 0,
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

        /// <summary>
        /// Register a configuration variable
        /// </summary>
        /// <param name="name">Name of variable</param>
        /// <param name="value">Value of variable</param>
        protected void RegisterVariable<T>(string name, T value)
        {
            _server.RegisterVariable(name, value);
        }

        /// <summary>
        /// Retrieve a configuration variable
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <param name="name">Name of variable</param>
        /// <param name="variable">Output variable</param>
        /// <returns>If the variable was found</returns>
        protected bool TryGetVariable<T>(string name, out T variable)
        {
            return _server.TryGetVariable(name, out variable);
        }

        protected MultipartFormData GetFormData(string name)
        {
            return _server.FormDataContent
                .Where(c => c.Name == name)
                .FirstOrDefault();
        }
    }
}
