using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    /// <summary>
    /// Concrete instance of ILogger interface
    /// </summary>
    public class Logger : ILogger
    {
        /// <summary>
        /// Terminator character to send after each log entry
        /// </summary>
        protected string terminator;

        /// <summary>
        /// Output action
        /// </summary>
        protected Action<string> outputAction;

        /// <summary>
        /// Singleton logger to print to console
        /// </summary>
        private static Logger _consoleLogger = null;
        public static Logger ConsoleLogger
        {
            get
            {
                if (_consoleLogger == null)
                {
                    _consoleLogger = new Logger(Console.WriteLine, Environment.NewLine);
                }

                return _consoleLogger;
            }
        }

        /// <summary>
        /// Singleton logger to print to debug window
        /// </summary>
        private static void DebugLog(string msg) => System.Diagnostics.Debug.WriteLine(msg);
        private static Logger _debugLogger = null;
        public static Logger DebugLogger
        {
            get
            {
                if (_debugLogger == null)
                {
                    _debugLogger = new Logger(DebugLog, Environment.NewLine);
                }

                return _debugLogger;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="terminator">Terminator character to be sent after each log entry</param>
        public Logger(string terminator = null) 
        {
            this.terminator = terminator;
        }

        /// <summary>
        /// Custom constructor
        /// </summary>
        /// <param name="outputAction">Custom output action to log</param>
        /// <param name="terminator">Terminator character to be sent after each log entry</param>
        public Logger(Action<string> outputAction, string terminator = null)
        {
            this.outputAction = outputAction;
            this.terminator = terminator;
        }

        /// <inheritdoc />
        public void Send(string message)
        {
            if (outputAction != null)
            {
                outputAction(message + terminator);
            }
        }

        /// <inheritdoc />
        public void Send(IDictionary<string, object> headers, Func<object, string> encoder = null)
        {
            Send(string.Join(Environment.NewLine, headers.Select(
                header => header.Key + ": " 
                    + encoder == null || header.Value == null
                        ? header.Value?.ToString()
                        : encoder(header.Value))));
        }

        /// <inheritdoc />
        public void Send(Exception e)
        {
            Send(e.Message);
        }

        /// <inheritdoc />
        public void Send(Message message)
        {
            Send(message.ToString());
        }

        /// <summary>
        /// Send encoded object
        /// </summary>
        /// <param name="obj">Object value</param>
        /// <param name="encoder">Custom encoder, defaults to ToString</param>
        public void Send(object obj, Func<object, string> encoder = null)
        {
            Send(encoder == null
                ? obj.ToString()
                : encoder(obj));
        }
    }
}
