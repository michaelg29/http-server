using HttpServer.Logging;
using HttpServer.Main;
using HttpServer.WebServer.Content;
using System;
using System.Threading.Tasks;
using static System.Net.Http.HttpMethod;
using static HttpServer.Main.Extensions;
using System.IO;
using System.Net;
using System.Net.Http;

namespace HttpServer.WebServer.Test
{
    public enum Version
    {
        V1_0,
        V1_1,
        V1_2,
        V1_3
    }

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

    public class SystemInfo
    {
        public DateTimeOffset curTime;
        public string version;
    }

    public interface IService
    {
        TestClassGreeting Generate(TestClass testClass);

        string GetVersionAsString(Version v);
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

        public string GetVersionAsString(Version v)
        {
            return v.ToString();
        }
    }

    public class ControllerTest : Controller
    {
        private IService _s;
        private ILogger _l;
        public ControllerTest(IService s, ILogger l)
        {
            _s = s;
            _l = l;
        }

        [ControllerEndpoint(HttpGet, "/controller/header")]
        public HttpResponseMessage TestMsg()
        {
            HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.OK);
            msg.Content = new StringContent("{\"hello\":15}", System.Text.Encoding.UTF8, "text/json");
            msg.Headers.Add("test", Request.Headers.Get("token"));
            return msg;
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

        [ControllerEndpoint(HttpGet, "/controller/info")]
        public async Task<SystemInfo> GetInfo(string url)
        {
            Console.WriteLine(url);
            return await Task.FromResult(new SystemInfo
            {
                curTime = TryGetVariable("date", out DateTime dt) ? dt : DateTime.MinValue,
                version = TryGetVariable("version", out Version v) ? _s.GetVersionAsString(v) : null
            });
        }

        [ControllerEndpoint(HttpGet, "/controller/throw")]
        public void ThrowException(int code = 0, string message = null)
        {
            throw new Exception($"{code}: {message}");
        }

        [ControllerEndpoint(HttpGet, "/controller/error/{msg}")]
        public View Error(string msg)
        {
            return new View("error2.html", msg.ToLower(), msg.ToUpper(), TryGetVariable("name", out string name) ? name : string.Empty);
        }

        [ControllerEndpoint(HttpGet, "/controller/send")]
        public void SendMessage(string msg)
        {
            _l.Send(new Message("Recieved data")
                .With("data", msg));
        }

        [ControllerEndpoint(HttpPost, "/controller/form")]
        public void ParseForm(string name, string grade, string password, string url)
        {
            Console.WriteLine($"name {name}, grade {grade}, password {password}, url {url}");
        }

        [ControllerEndpoint(HttpPost, "/controller/multipart")]
        public async void MultipartFormAsync()
        {
            Console.WriteLine(await GetMultipartFormString("dsaffsd"));
        }

        [ControllerEndpoint(HttpGet, "/controller/upload")]
        public View GetFileForm()
        {
            return new View("file.html");
        }

        [ControllerEndpoint(HttpPost, "/controller/print")]
        public async void PrintFormValsAsync()
        {
            string[] keys = new string[]
            {
                "filename",
                "key1",
                "key13",
                "hello_to_you",
                "multipart_val_3",
                "long"
            };
            foreach (string key in keys)
            {
                Console.WriteLine($"{key} => {await GetMultipartFormString(key)}");
            }
        }

        [ControllerEndpoint(HttpPost, "/controller/upload")]
        public async void UploadBuffer(string filename, byte[] data)
        {
            FileStream f = File.OpenWrite(filename);
            f.Write(data, 0, data.Length);
            f.Close();
            SetResponse(responseCode: HttpStatusCode.OK);
        }
    }

    class Program
    {
        static WebServer ws;

        static string ExceptionHandler(HttpListenerContext context, Exception e, IConfigurationStore config)
        {
            return new Message(e)
                .With("route", context.Request.Url)
                .ToString();
        }

        static View NotFoundHandler(HttpListenerContext context, IConfigurationStore config)
        {
            return new View("error2.html", "Did not find", context.Request.Url, config.GetVariable("name", string.Empty));
        }

        async static Task Main(string[] args)
        {
            int res = 0;

            ws = new WebServer(logger: Logger.ConsoleLogger, config: new WebServerConfig
            {
                HostUrl = "http://+:8080;https://+:8443",
            });
            ws.RegisterVariable("name", "Web server test");
            ws.RegisterVariable("version", Version.V1_0);
            ws.RegisterVariable("date", DateTime.UtcNow);

            ws.RegisterService<IService, Service>();
            ws.RegisterController<ControllerTest>();

            ws.SetExceptionHandler<string>(ExceptionHandler);
            ws.SetNotFoundHandler<View>(NotFoundHandler);

            res = await ws.RunAsync();

            Console.WriteLine($"Ended with code {res}, press any key to continue...");
            Console.ReadKey();
        }
    }
}
