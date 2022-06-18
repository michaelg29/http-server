using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer.WebServer;
using ParamList = System.Collections.Generic.IDictionary<string, string>;

namespace HttpServer.WebServer.Test
{
    class Program
    {
        public static void Action_(WebServer ws)
        {
            Console.WriteLine("Action_");
        }

        public static void Action_Hello(WebServer ws)
        {
            Console.WriteLine("Action_Hello");
        }
        public static void Action_int(WebServer ws, int arg0)
        {
            Console.WriteLine($"Action_int: {arg0}");
        }

        public static void Action_hello_test_int_str(WebServer ws, int arg0, string arg1, ParamList paramList)
        {
            if (paramList != null && paramList.Count > 0)
            {
                Console.WriteLine($"Action_hello_test_int_str with params: {arg0}, {arg1}, " +
                    string.Join(", ", paramList.Select(kvp => $"{kvp.Key} = {kvp.Value}")));
            }
            else
            {
                Console.WriteLine($"Action_hello_test_int_str: {arg0}, {arg1}");
            }
        }

        static void Main(string[] args)
        {
            RouteTree rt = new RouteTree();
            rt.AddRoute("/", Action_);
            rt.AddRoute("/hello", Action_Hello);
            rt.AddRoute<int>("/{int}", Action_int);
            rt.AddRoute<int, string, ParamList>("/hello/test/{int}/{str}", Action_hello_test_int_str);

            Console.WriteLine(rt.Navigate(null, "/"));
            Console.WriteLine(rt.Navigate(null, "/hello"));
            Console.WriteLine(rt.Navigate(null, "/5"));
            Console.WriteLine(rt.Navigate(null, "/123"));
            Console.WriteLine(rt.Navigate(null, "/123.5"));
            Console.WriteLine(rt.Navigate(null, "/hello/test/15/adls;kfj"));
            Console.WriteLine(rt.Navigate(null, "/hello/test/15/adls;kfj?hello=%20%20thisisarg"));

            //List<int> nums = new List<int>();
            //for (int i = 0; i < 15; i++)
            //{
            //    nums.Add(i);
            //    string str = $@"        public void AddRoute<{string.Join(", ", nums.Select(n => $"T{n}"))}>
            //(string route, Action<WebServer, {string.Join(", ", nums.Select(n => $"T{n}"))}> action)
            //=> _AddRoute(route, (ws, obj) 
            //    => action(ws, {string.Join(", ", nums.Select(n => $"obj.Get<T{n}>({n})"))}));";

            //    Console.WriteLine(str + Environment.NewLine);
            //}
        }
    }
}
