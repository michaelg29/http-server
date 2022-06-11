using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    public interface ILogger
    {
        void Send(string message);
        void Send(IDictionary<string, object> headers, Func<object, string> decoder = null);
        void Send(Exception e);
        void Send(Message message);
    }
}
