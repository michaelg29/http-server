using HttpServer.Logging;
using HttpServer.WebServer;
using System.Threading.Tasks;

namespace Test
{
    class Simple
    {
        async static Task Main(string[] args)
        {
            WebServer ws = new WebServer(logger: Logger.ConsoleLogger, config: new WebServerConfig
            {
                HostUrl = "http://localhost:8080",
                ErrorPath = "error2.html"
            });
            ws.RegisterVariable("name", "Web server test");
            ws.RegisterVariable("version", Version.V1_0);
            ws.RegisterVariable("date", DateTime.UtcNow);
        }
    }
}