using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer.Client.Test
{
    class TestClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public int Grade { get; set; }
        public string Home { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var person = new TestClass
            {
                Name = "Michael",
                Age=18,
                Grade=15,
                Home="Antarctica"
            };

            var json = JsonConvert.SerializeObject(person);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var url = "http://localhost:8080/greet";
            var client = new HttpClient();

            var response = await client.PostAsync(url, data);

            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
        }
    }
}
