using System;
using System.Collections.Generic;
using HttpServer.WebServer;

namespace HttpServer.WebServer.Test
{
    class Program
    {
        public static void Action_(WebServer ws, params object[] args) {}
        public static void Action_Hello(WebServer ws, params object[] args) { }
        public static void Action_int(WebServer ws, params object[] args) { }
        public static void Action_Hello_test_int_str(WebServer ws, params object[] args) { }

        static void Main(string[] args)
        {
            RouteTree rt = new RouteTree();
            rt.AddRoute("/", Action_);
            rt.AddRoute("/hello", Action_Hello);
            rt.AddRoute("/{int}", Action_int);
            rt.AddRoute("/Hello/test/{int}/{str}", Action_Hello_test_int_str);
        }
    }
}
