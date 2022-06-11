using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    public class Logger : ILogger
    {
        protected string terminator;
        protected Action<string> outputAction;

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

        public Logger(string terminator = null) 
        {
            this.terminator = terminator;
        }

        public Logger(Action<string> outputAction, string terminator = null)
        {
            this.outputAction = outputAction;
            this.terminator = terminator;
        }

        public void Send(string message)
        {
            if (outputAction != null)
            {
                outputAction(message + terminator);
            }
        }

        public void Send(IDictionary<string, object> headers, Func<object, string> decoder = null)
        {
            Send(string.Join(Environment.NewLine, headers.Select(
                header => header.Key + ": " + decoder == null
                    ? header.Value.ToString()
                    : decoder(header.Value))));
        }

        public void Send(Exception e)
        {
            Send(e.Message);
        }

        public void Send(Message message)
        {
            Send(message.ToString());
        }

        public void Send(object obj, Func<object, string> decoder = null)
        {
            Send(decoder == null
                ? obj.ToString()
                : decoder(obj));
        }
    }
}
