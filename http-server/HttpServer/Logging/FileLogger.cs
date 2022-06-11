using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    public class FileLogger : Logger
    {
        private string filePath;
        public FileLogger(string fileName, bool includeDate = false, string terminator = null)
            : base(terminator)
        {
            filePath = includeDate
                ? $"{DateTime.UtcNow.ToString("yyyy-dd-M--HH-mm-ss")}_{fileName}"
                : fileName;

            outputAction = this.Output;
        }

        public void Output(string msg)
        {
            File.AppendAllText(filePath, msg);
        }
    }
}
