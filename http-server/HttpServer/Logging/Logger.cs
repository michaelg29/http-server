using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    public class Logger : ILogger
    {
        private Action<string> outputAction;

        public Logger(Action<string> outputAction)
        {
            this.outputAction = outputAction;
        }

        public void Send(string message)
        {
            outputAction(message);
        }

        public void Send(IDictionary<string, object> headers)
        {

        }

        public void Send(Exception e)
        {

        }

        public void Send(Message message)
        {

        }
    }
}
