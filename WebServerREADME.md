# Web Server
The web server library allows users to program custom web applications containing endpoints linked to controller methods.
## Usage
### Library files
In your C# project, you must point to the three library files: `HttpServer.Main.dll`, `HttpServer.Logging.dll`, and `HttpServer.WebServer.dll` in the `/bin` directory.
## Examples
All these examples can be found in the `Examples` directory.
### Simple example
```
using HttpServer.Logging;
using HttpServer.WebServer;
using System.Threading.Tasks;

namespace Test
{
    class SimpleProgram
    {
        async static Task Main(string[] args)
        {
            WebServer ws = new WebServer(logger: Logger.ConsoleLogger, config: new WebServerConfig
            {
                hostUrl: "http://localhost:8080/"
            });

            await ws.RunAsync();
        }
    }
}
```

This code will start the web server and tell it to listen at localhost port 8080. There are no associated endpoints, so any requests will result in an error file.
### Static Endpoints Example
### Controller Example
### Controller With Services Example
### Custom Configuration
```
WebServer ws = new WebServer(logger: Logger.ConsoleLogger, config: new WebServerConfig
{
    HostUrl = "http://localhost:8080",
    ErrorPath = "error2.html"
});
ws.RegisterVariable("name", "Web server test");
ws.RegisterVariable("version", Version.V1_0);
ws.RegisterVariable("date", DateTime.UtcNow);
```