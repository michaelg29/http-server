using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    /// <summary>
    /// Public interface to be inherited by server instances
    /// </summary>
    public interface IHttpServer
    {
        /// <summary>
        /// Get the current request object
        /// </summary>
        HttpListenerRequest Request
        {
            get;
        }

        /// <summary>
        /// Response code for current response
        /// </summary>
        HttpStatusCode ResponseCode
        {
            set;
        }

        /// <summary>
        /// Content length for current response
        /// </summary>
        long ContentLength
        {
            set;
        }

        /// <summary>
        /// Content type for current response
        /// </summary>
        string ContentType
        {
            set;
        }

        /// <summary>
        /// Encoding for the current response
        /// </summary>
        Encoding Encoding
        {
            set;
        }

        /// <summary>
        /// Register a configuration variable
        /// </summary>
        /// <param name="name">Name of variable</param>
        /// <param name="value">Value of variable</param>
        void RegisterVariable<T>(string name, T value);

        /// <summary>
        /// Retrieve a configuration variable
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <param name="name">Name of variable</param>
        /// <param name="variable">Output variable</param>
        /// <returns>If the variable was found</returns>
        bool TryGetVariable<T>(string name, out T variable);

        /// <summary>
        /// Send a file to the current response
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="args">Formatting arguments</param>
        Task SendFileAsync(string filePath, params string[] args);

        /// <summary>
        /// Send string content
        /// </summary>
        /// <param name="content">String content</param>
        /// <param name="args">Formatting arguments</param>
        Task SendStringAsync(string content, string contentType, params string[] args);

        /// <summary>
        /// Run the server
        /// </summary>
        /// <param name="args">Input arguments</param>
        /// <returns>Exit code</returns>
        Task<int> RunAsync(string[] args);

        /// <summary>
        /// Shutdown the server
        /// </summary>
        void Shutdown();
    }
}
