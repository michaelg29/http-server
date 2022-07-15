using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer
{
    public class View
    {
        public string Filepath { get; set; }

        public string MimeType { get; set; }

        public string[] Args { get; set; }

        public View(string filepath, params object[] args)
        {
            Filepath = filepath;
            MimeType = Mime.GetMimeType(filepath);
            Args = args.Select(arg => arg.ToString()).ToArray();
        }

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
