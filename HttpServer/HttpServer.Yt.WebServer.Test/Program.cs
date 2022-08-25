using HttpServer.Logging;
using HttpServer.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Yt.WebServer.Test
{
    class Item
    {
        public int id;
        public string name;
    }

    class ItemController : Controller
    {
        [HttpGet("/item/{id:int}")]
        public Item GetItem(int id)
        {
            Console.WriteLine($"Get item {id}");
            return new Item
            {
                id = id,
                name = "Item " + id,
            };
        }
    }

    class ServerController : Controller
    {
        [HttpGet("/shutdown/{key}")]
        public void Shutdown(string key)
        {
            if (key == "hello")
            {
                this.Shutdown();
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            WebServer ws = new WebServer("http://+:8080/", "", Logger.ConsoleLogger);

            ws.RegisterController(new ItemController());
            ws.RegisterController(new ServerController());

            Console.WriteLine(await ws.RunAsync());
            Console.ReadKey();
        }
    }
}
