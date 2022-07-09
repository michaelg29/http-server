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
    public class TestClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public int Grade { get; set; }
        public string Home { get; set; }
    }

    public class TestClassGreeting
    {
        public string Greeting { get; set; }
    }

    public interface IService
    {
        TestClassGreeting Generate(TestClass testClass);
    }

    public class Service : IService
    {
        public TestClassGreeting Generate(TestClass testClass)
        {
            return new TestClassGreeting
            {
                Greeting = $"Hello, {testClass.Name} ({testClass.Age}), welcome to grade {testClass.Grade}, I hope the journey from {testClass.Home} wasn't too difficult."
            };
        }
    }


    public class ControllerTest : Controller
    {
        private IService _s;
        public ControllerTest(IService s)
        {
            _s = s;
        }

        [ControllerEndpoint(HttpGet, "/controller/hello")]
        public void TestEndpoint()
        {
            SetResponse(responseCode: System.Net.HttpStatusCode.NoContent);
            Console.WriteLine("Hello, from controller");
        }

        [ControllerEndpoint(HttpPatch, "/controller/testbody")]
        public async Task<TestClassGreeting> PatchBody(TestClass testClass)
        {
            return await Task.FromResult(_s.Generate(testClass));
        }

        [ControllerEndpoint(HttpPut, "/controller/put")]
        public string PutController(TestClass testClass)
        {
            SetResponse(contentType: "text/html");
            return string.Format("<p>Greeting: {0}</p>", _s.Generate(testClass).Greeting);
        }

        [ControllerEndpoint(HttpPost, "/controller/shutdown")]
        public string PostShutdown()
        {
            Shutdown();
            return "Goodbye";
        }

        [ControllerEndpoint(HttpGet, "/controller/startdate")]
        public DateTimeOffset GetStartdate()
        {
            return TryGetVariable<DateTime>("date", out var date) ? date : DateTimeOffset.UtcNow;
        }
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
            return await Task.FromResult(Action_greet(name, age, grade, home));
        }

        static TestClassGreeting Action_greet(string name, int age, int grade, string home)
        {
            return new TestClassGreeting
            {
                Greeting = $"Hello, {name} ({age}), welcome to grade {grade}, I hope the journey from {home} wasn't too difficult."
            };
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
            ws.RegisterVariable("version", 1);
            ws.RegisterVariable("date", DateTime.UtcNow);

            ws.RouteTree.AddRoute(Get, "/", Action_);
            ws.RouteTree.AddRoute(Get, "/hello", Action_Hello);
            ws.RouteTree.AddRoute<int>(Get, "/{num:int}", Action_int);
            ws.RouteTree.AddRoute<int, string>(Get, "/hello/test/{num:int}/{str}", Action_hello_test_int_str);
            ws.RouteTree.AddRoute<string, int>(Get, "/hello/query", Action_hello_query);
            ws.RouteTree.AddRoute<TestClass>(Get, "/greetjson", Action_body);
            ws.RouteTree.AddRoute<string, int, int, string>(Get, "/greetasync", Action_body);
            ws.RouteTree.AddRoute<string, int, int, string, TestClassGreeting>(Get, "/greet", Action_greet);
            ws.RouteTree.AddRoute<int, int>(Get, "/increment", Action_increment);
            ws.RouteTree.AddRoute<int, int, int>(Get, "/subtract", Action_subtract);

            ws.RegisterService<IService, Service>();
            ws.RegisterController<ControllerTest>();

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
