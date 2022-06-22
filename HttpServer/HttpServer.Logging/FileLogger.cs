using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    /// <summary>
    /// Logger instance to send log messages to a file
    /// </summary>
    public class FileLogger : Logger
    {
        /// <summary>
        /// Output file path for this instance
        /// </summary>
        private string filePath;

        /// <summary>
        /// Initialize file 
        /// </summary>
        /// <param name="fileName">Name of file to output to</param>
        /// <param name="includeDate">Whether to include the date in the output file name</param>
        /// <param name="terminator">Terminator character after each log entry, defaults to Environment.NewLine</param>
        public FileLogger(string fileName, bool includeDate = false, string terminator = null)
            : base(terminator ?? Environment.NewLine)
        {
            // construct file name
            filePath = includeDate
                ? $"{DateTime.UtcNow.ToString("yyyy-dd-M--HH-mm-ss")}_{fileName}"
                : fileName;

            // set output action
            outputAction = this.Output;
        }

        /// <summary>
        /// Custom output function for this logger
        /// </summary>
        /// <param name="msg">Message to output</param>
        public void Output(string msg)
        {
            File.AppendAllText(filePath, msg);
        }
    }
}
