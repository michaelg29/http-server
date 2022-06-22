using HttpServer;
using HttpServer.Logging;
using System;

namespace HttpServer.Test
{
    enum TestEnum
    {
        TestEnum1 = 0,
        TestEnum2 = 1
    }

    class Program
    {
        public static void Main(string[] args)
        {
            Message m = new Message("Title");
            m.With("value")
                .With(new ArithmeticException("test arithmetic"))
                .With(TestEnum.TestEnum1)
                .With("header1", "value1");

            Logger.ConsoleLogger.Send(m);
            Logger.ConsoleLogger.Send(m);

            ILogger logger = new FileLogger("test.txt", true);
            logger.Send(m);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
