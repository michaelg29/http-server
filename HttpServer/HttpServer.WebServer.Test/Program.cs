using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Http.HttpMethod;
using static HttpServer.Main.Extensions;

namespace HttpServer.WebServer.Test
{
    class Program
    {
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

        static void Main(string[] args)
        {
            object val;
            Type type;
            string[] vals =
            {
                "15",
                "57.5",
                "true",
                "a;sdgau35hi4321u",
                Guid.NewGuid().ToString()
            };
            foreach (string s in vals)
            {
                if (ArgType.TryParse(s, out val, out type))
                {
                    Console.WriteLine($"{s} => {type.Name}");
                }
                else
                {
                    Console.WriteLine($"Could not parse {s}");
                }
            }

            RouteTree rt = new RouteTree();
            rt.AddRoute(Get, "/", Action_);
            rt.AddRoute(Get, "/hello", Action_Hello);
            rt.AddRoute<int>(Put, "/{int}", Action_int);
            rt.AddRoute<int, string>(Post, "/hello/test/{int}/{str}", Action_hello_test_int_str);

            Console.WriteLine(rt.TryNavigate(Get, "/"));
            Console.WriteLine(rt.TryNavigate(Put, "/"));
            Console.WriteLine(rt.TryNavigate(Get, "/hello"));
            Console.WriteLine(rt.TryNavigate(Put, "/15"));
            Console.WriteLine(rt.TryNavigate(Delete, "/15"));
            Console.WriteLine(rt.TryNavigate(Patch, "/tegfdv"));
            Console.WriteLine(rt.TryNavigate(Trace, "/15.5"));
            Console.WriteLine(rt.TryNavigate(Post, "/hello/test/17/asdg"));

            //List<int> nums = new List<int>();
            //for (int i = 0; i < 16; i++)
            //{
            //    nums.Add(i);
            //    string str = $@"        public void AddRoute<{string.Join(", ", nums.Select(n => $"T{n}"))}>
            //(HttpMethod method, string route, Action<{string.Join(", ", nums.Select(n => $"T{n}"))}> action)
            //    => _AddRoute(method, route, (obj) 
            //        => action({string.Join(", ", nums.Select(n => $"obj.Get<T{n}>({n})"))}));";

            //    Console.WriteLine(str + Environment.NewLine);
            //}
        }
    }
}
