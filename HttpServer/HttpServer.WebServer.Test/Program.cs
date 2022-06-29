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

    class TestClassGreeting
    {
        public string Greeting { get; set; }
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

        static async Task<TestClassGreeting> Action_body(string name, int age, int grade, string home)
        {
            return await Task.FromResult(new TestClassGreeting
            {
                Greeting = $"Hello, {name} ({age}), welcome to grade {grade}, I hope the journey from {home} wasn't too difficult."
            });
        }

        static async Task<TestClassGreeting> Action_body(TestClass testClass)
        {
            return await Action_body(testClass.Name, testClass.Age, testClass.Grade, testClass.Home);
        }

        static async Task<int> Action_increment(int a, int b)
        {
            return await Task.FromResult(a + b);
        }

        static int Action_subtract(int a, int b)
        {
            return a - b;
        }

        async static Task Main(string[] args)
        {
            int res = 0;

            ws = new WebServer("http://localhost:8080/", null, Logger.ConsoleLogger);
            ws.RouteTree.AddRoute(Get, "/", Action_);
            ws.RouteTree.AddRoute(Get, "/hello", Action_Hello);
            ws.RouteTree.AddRoute<int>(Get, "/{num:int}", Action_int);
            ws.RouteTree.AddRoute<int, string>(Get, "/hello/test/{num:int}/{str}", Action_hello_test_int_str);
            ws.RouteTree.AddRoute<string, int>(Get, "/hello/query", Action_hello_query);
            ws.RouteTree.AddRoute<TestClass>(Post, "/greet", Action_body);
            ws.RouteTree.AddRoute<string, int, int, string>(Get, "/greet", Action_body);
            ws.RouteTree.AddRoute<int, int>(Get, "/increment", Action_increment);
            ws.RouteTree.AddRoute<int, int, int>(Get, "/subtract", Action_subtract);

            res = await ws.RunAsync(args);

            //List<int> nums = new List<int>();
            //for (int i = 0; i < 16; i++)
            //{
            //    nums.Add(i);
            //    string str = $@"        public void AddRoute<{string.Join(", ", nums.Select(n => $"T{n}"))}, Tret>
            //(HttpMethod method, string route, Func<{string.Join(", ", nums.Select(n => $"T{n}"))}, Tret> function)
            //    => _AddRoute(method, route, function);";

            //    Console.WriteLine(str + Environment.NewLine);
            //}

            Console.WriteLine($"Ended with code {res}, press any key to continue...");
            Console.ReadKey();
        }
    }
}
