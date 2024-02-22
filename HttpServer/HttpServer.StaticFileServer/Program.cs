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

            bool https = false;
            bool local = false;
            bool allowUpload = false;
            string hostDir = string.Empty;
            string passcode = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                if (i < args.Length - 1)
                {
                    // valued argument
                    string value = args[i + 1];
                    if (args[i] == "-d" || args[i] == "--dir")
                    {
                        hostDir = value;
                        i += 1;
                    }
                    else if (args[i] == "-p" || args[i] == "--pass")
                    {
                        https = true;
                        passcode = value;
                        i += 1;
                    }
                }

                if (args[i] == "-s" || args[i] == "--https")
                {
                    https = true;
                }
                else if (args[i] == "-l" || args[i] == "--local")
                {
                    local = true;
                }
                else if (args[i] == "-u" || args[i] == "--upload")
                {
                    allowUpload = true;
                }
            }

            // construct URL
            string ip = local ? "localhost" : "+";
            string hostUrl = $"https://{ip}:8443/";
            if (!https)
            {
                hostUrl += $";http://{ip}:8080/";
            }
            Console.WriteLine($"Hosting at address {hostUrl}");

            // run server
            StaticFileServer server = new StaticFileServer(hostUrl, hostDir, allowUpload, Logger.ConsoleLogger);
            int res = await server.RunAsync();

            Console.WriteLine($"Ended with code {res}, press any key to continue...");
            Console.ReadKey();
        }
    }
}
