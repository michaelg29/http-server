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
        Task SendStringAsync(string content, params string[] args);

        /// <summary>
        /// Run the server
        /// </summary>
        /// <param name="args">Input arguments</param>
        /// <returns>Exit code</returns>
        Task<int> RunAsync(string[] args);
    }
}
