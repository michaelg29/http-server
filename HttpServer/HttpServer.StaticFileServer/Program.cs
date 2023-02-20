using HttpServer.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.StaticFileServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, world!");

            string hostUrl = "http://+:8080/";
            string hostDir = string.Empty;
            bool allowUpload = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (i < args.Length - 1)
                {
                    // valued argument
                    string value = args[i + 1];
                    if (args[i] == "-u")
                    {
                        hostUrl = value.EndsWith("/")
                            ? value
                            : value + "/";
                    }
                    else if (args[i] == "-d")
                    {
                        hostDir = value;
                    }
                }

                if (args[i] == "-p")
                {
                    allowUpload = true;
                }
            }

            StaticFileServer server = new StaticFileServer(hostUrl, hostDir, allowUpload, Logger.ConsoleLogger);
            int res = await server.RunAsync();

            Console.WriteLine($"Ended with code {res}, press any key to continue...");
            Console.ReadKey();
        }
    }
}
