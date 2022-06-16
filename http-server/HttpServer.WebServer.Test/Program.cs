using System;
using System.Collections.Generic;
using HttpServer.WebServer;

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

        public static void Action_hello_test_int_str(WebServer ws, int arg0, string arg1)
        {
            Console.WriteLine($"Action_hello_test_int_str: {arg0}, {arg1}");
        }

        static void Main(string[] args)
        {
            RouteTree rt = new RouteTree();
            rt.AddRoute("/", (ws, args) => Action_(ws));
            rt.AddRoute("/hello", (ws, args) => Action_Hello(ws));
            rt.AddRoute("/{int}", (ws, args) => Action_int(ws, (int)args[0]));
            rt.AddRoute("/hello/test/{int}/{str}", (ws, args) => Action_hello_test_int_str(ws, (int)args[0], (string)args[1]));

            Console.WriteLine(rt.Navigate(null, "/"));
            Console.WriteLine(rt.Navigate(null, "/hello"));
            Console.WriteLine(rt.Navigate(null, "/5"));
            Console.WriteLine(rt.Navigate(null, "/123"));
            Console.WriteLine(rt.Navigate(null, "/123.5"));
            Console.WriteLine(rt.Navigate(null, "/hello/test/15/adls;kfj"));
        }
    }
}
