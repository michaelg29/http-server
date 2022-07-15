using HttpServer.Logging;
using HttpServer.WebServer;
using System.Threading.Tasks;

namespace Test
{
    class Simple
    {
        async static Task Main(string[] args)
        {
            WebServer ws = new WebServer("http://localhost:8080", logger: Logger.ConsoleLogger);

            await ws.RunAsync();
        }
    }
}