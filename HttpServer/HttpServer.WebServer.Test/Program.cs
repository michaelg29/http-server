using HttpServer.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Http.HttpMethod;
using static HttpServer.Main.Extensions;

namespace HttpServer.WebServer.Test
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
        static WebServer ws;

        static void Action_()
        {
            Console.WriteLine("Action_");
        }

        static void Action_Hello()
        {
            Console.WriteLine("Action_Hello");
        }

        static void Action_int(int num)
        {
            Console.WriteLine($"Action_int: {num}");
        }

        static void Action_hello_test_int_str(int num, string str)
        {
            Console.WriteLine($"Action_hello_test_int_str: {num}, {str}");
        }

        static async void Action_hello_query(string name, int age)
        {
            await ws.SendStringAsync($"Hello, {name}, with age {age}");
        }

        static async Task Action_body(TestClass testClass)
        {
            await ws.SendStringAsync($"Hello, {testClass.Name} ({testClass.Age}), welcome to grade {testClass.Grade}, I hope the journey from {testClass.Home} wasn't too difficult.");
        }

        async static Task Main(string[] args)
        {
            //object val;
            //Type type;
            //string[] vals =
            //{
            //    "15",
            //    "57.5",
            //    "true",
            //    "a;sdgau35hi4321u",
            //    Guid.NewGuid().ToString()
            //};
            //foreach (string s in vals)
            //{
            //    if (ArgType.TryParse(s, out val, out type))
            //    {
            //        Console.WriteLine($"{s} => {type.Name}");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"Could not parse {s}");
            //    }
            //}

            ws = new WebServer("http://+:8080/", null, Logger.ConsoleLogger);
            ws.RouteTree.AddRoute(Get, "/", Action_);
            ws.RouteTree.AddRoute(Get, "/hello", Action_Hello);
            ws.RouteTree.AddRoute<int>(Get, "/{num:int}", Action_int);
            ws.RouteTree.AddRoute<int, string>(Get, "/hello/test/{num:int}/{str}", Action_hello_test_int_str);
            ws.RouteTree.AddRoute<string, int>(Get, "/hello/query", Action_hello_query);
            ws.RouteTree.AddRoute<TestClass>(Get, "/greet", Action_body);
            await ws.RunAsync(args);

            //List<int> nums = new List<int>();
            //for (int i = 0; i < 16; i++)
            //{
            //    nums.Add(i);
            //    string str = $@"        public void AddRoute<{string.Join(", ", nums.Select(n => $"T{n}"))}>
            //(HttpMethod method, string route, Action<{string.Join(", ", nums.Select(n => $"T{n}"))}> action)
            //    => _AddRoute(method, route, action);";

            //    Console.WriteLine(str + Environment.NewLine);
            //}
        }
    }
}
