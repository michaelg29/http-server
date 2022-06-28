using HttpServer.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer
{
    public class WebServer : Main.HttpServer
    {
        /// <inheritdoc />
        public WebServer(string hostUrl = null, string hostDir = null, ILogger logger = null)
            : base(hostUrl, hostDir, logger) { }

        /// <summary>
        /// Process current request
        /// </summary>
        protected override async Task ProcessRequest()
        {
            
        }

        /// <inheritdoc />
        protected override async Task<string> GetCommand()
        {
            return await Task.FromResult(Console.ReadLine());
        }
    }
}
