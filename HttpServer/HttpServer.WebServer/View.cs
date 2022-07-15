using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer
{
    /// <summary>
    /// Class representing a file to be sent as a response.
    /// </summary>
    public class View
    {
        /// <summary>
        /// Path to the file.
        /// </summary>
        public string Filepath { get; set; }

        /// <summary>
        /// Mime type of the view.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Formatting arguments.
        /// </summary>
        public string[] Args { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filepath">Filepath</param>
        /// <param name="args">Formatting arguments</param>
        public View(string filepath, params object[] args)
        {
            Filepath = filepath;
            MimeType = Mime.GetMimeType(filepath);
            Args = args.Select(arg => arg.ToString()).ToArray();
        }

        /// <summary>
        /// Execute the view by reading the file and applying the arguments
        /// </summary>
        /// <returns>Formatted string</returns>
        public string Format()
        {
            string content = null;
            string formatted = null;
            try
            {
                formatted = File.ReadAllText(Filepath);
                if (Args?.Count() > 0)
                {
                    formatted = string.Format(formatted, Args);
                }
            }
            finally
            {
                content = formatted;
            }

            return content;
        }
    }
}
