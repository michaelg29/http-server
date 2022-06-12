using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    /// <summary>
    /// Public logger interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Send string message
        /// </summary>
        /// <param name="message">String message</param>
        void Send(string message);

        /// <summary>
        /// Send headers
        /// </summary>
        /// <param name="headers">Headers mapping keys to objects</param>
        /// <param name="decoder">Custom encoder function, defaults to ToString</param>
        void Send(IDictionary<string, object> headers, Func<object, string> decoder = null);

        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="e">Exception object</param>
        void Send(Exception e);

        /// <summary>
        /// Log message
        /// </summary>
        /// <param name="message">Message object</param>
        void Send(Message message);
    }
}
