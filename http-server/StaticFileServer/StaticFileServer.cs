using HttpServer;
using HttpServer.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StaticFileServer
{
    public class StaticFileServer : IHttpServer
    {
        private string hostUrl;
        private string hostDir;
        private string AbsolutePath(string route)
        {
            return (route.StartsWith("/") || hostDir.EndsWith("/"))
                ? $"{hostDir}{route}"
                : $"{hostDir}/{route}";
        }

        private string errorPath = "error.html";
        private string notFoundPath = "notFound.html";
        private string dirPath = "directory.html";
        private string shutdownPath = "shutdown.html";

        private bool running = false;
        private HttpListener listener = null;
        private HttpListenerContext ctx = null;
        private ILogger logger = null;

        public HttpListenerRequest Request
        {
            get => ctx.Request;
        }

        public HttpStatusCode ResponseCode
        {
            set
            {
                if (ctx != null)
                {
                    ctx.Response.StatusCode = (int)value;
                }
            }
        }

        public long ContentLength
        {
            set
            {
                if (ctx != null)
                {
                    ctx.Response.ContentLength64 = value;
                }
            }
        }

        public string ContentType
        {
            set
            {
                if (ctx != null)
                {
                    ctx.Response.ContentType = value;
                }
            }
        }

        public Encoding Encoding
        {
            set
            {
                if (ctx != null)
                {
                    ctx.Response.ContentEncoding = value;
                }
            }
        }

        public string FileExt
        {
            set
            {
                if (ctx != null)
                {
                    ContentType = Mime.GetMimeType(value);
                }
            }
        }

        public StaticFileServer(string hostUrl, string hostDir, ILogger logger)
        {
            this.hostUrl = hostUrl;
            this.hostDir = hostDir;
            this.logger = logger;
        }

        public async Task SendFileAsync(string filePath, params string[] args)
        {
            await _SendFileAsync(AbsolutePath(filePath), args);
        }

        private async Task _SendFileAsync(string filePath, params string[] args)
        {
            try
            {
                if (args.Count() > 0)
                {
                    await ReadFormattedFileToResponseAsync(filePath, args);
                }
                else
                {
                    await ReadFileToResponseAsync(filePath);
                }
            }
            catch (HttpListenerException e)
            {
                ResponseCode = HttpStatusCode.InternalServerError;
                Console.WriteLine($"HttpException: {e.Message}");
            }
            catch (Exception e)
            {
                ResponseCode = HttpStatusCode.InternalServerError;
                Console.WriteLine($"{e.GetType()}: {e.Message}");
                await ReadFormattedFileToResponseAsync(errorPath,
                    e.GetType().ToString(), e.Message);
            }
        }

        private async Task ReadFileToResponseAsync(string filePath)
        {
            await ctx.Response.OutputStream.FlushAsync();
            Stream input = new FileStream(filePath, FileMode.Open);

            try
            {
                ContentLength = input.Length;
                FileExt = filePath;
                Encoding = Encoding.UTF8;

                byte[] buffer = new byte[65536];
                int noBytes;
                while ((noBytes = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await ctx.Response.OutputStream.WriteAsync(buffer, 0, noBytes);
                    await ctx.Response.OutputStream.FlushAsync();
                }
            }
            finally
            {
                input.Close();
            }
        }

        private async Task ReadFormattedFileToResponseAsync(string filePath, params string[] args)
        {
            await ctx.Response.OutputStream.FlushAsync();

            string content = File.ReadAllText(filePath);
            try
            {
                string formatted = string.Format(content, args);
                content = formatted;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not format file: {e.Message}");
            }

            ContentLength = content.Length;
            FileExt = filePath;
            Encoding = Encoding.UTF8;

            byte[] buffer = Encoding.UTF8.GetBytes(content);
            await ctx.Response.OutputStream.WriteAsync(buffer, 0, content.Length);
            await ctx.Response.OutputStream.FlushAsync();
        }

        private async Task ProcessRequest()
        {
            string route = ctx.Request.Url.AbsolutePath;
            if (route == "/shutdown")
            {
                running = false;
                ctx.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            string absolutePath = AbsolutePath(route);

            if (File.Exists(absolutePath))
            {
                await _SendFileAsync(absolutePath);
            }
            else if (Directory.Exists(absolutePath))
            {
                string directoryList = string.Empty;
                string fileList = string.Empty;

                if (!route.EndsWith("/"))
                {
                    route += "/";
                }

                directoryList = string.Join("", Directory.GetDirectories(absolutePath)
                    .Select(s =>
                    {
                        int idx = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
                        string relPath = s.Substring(idx + 1);
                        return $"\n\t\t<ul><a href=\"{route}{relPath}\">{relPath}</a></ul>";
                    }));
                if (route.Length > 1)
                {
                    int n = route.Length - 2;
                    int idx = Math.Max(route.LastIndexOf('/', n), route.LastIndexOf('\\', n));
                    string parentPath = route.Substring(0, idx + 1);
                    directoryList += $"\n\t\t<ul><a href=\"{parentPath}\">..</a></ul>";
                }

                fileList = string.Join("", Directory.GetFiles(absolutePath)
                    .Select(s =>
                    {
                        int idx = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
                        string relPath = s.Substring(idx + 1);
                        return $"\n\t\t<ul><a href=\"{route}{relPath}\">{relPath}</a></ul>";
                    }));

                await _SendFileAsync(dirPath,
                    route, directoryList, fileList);
            }
            else
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await _SendFileAsync(notFoundPath);
            }
        }

        private async void ProcessContextAsync(IAsyncResult result)
        {
            try
            {
                ctx = this.listener.EndGetContext(result);

                // log the request
                logger.Send(new Message("Request")
                    .With("Url", ctx.Request.Url)
                    .With("Method", ctx.Request.HttpMethod)
                    .With("Host", ctx.Request.UserHostName)
                    .With("Agent", ctx.Request.UserAgent));

                await ProcessRequest();

                // log the response
                logger.Send(new Message("Response")
                    .With("Code", ctx.Response.StatusCode)
                    .With("Type", ctx.Response.ContentType ?? null)
                    .With("Length", ctx.Response.ContentLength64));

                ctx.Response.Close();
                ctx = null;

                if (running)
                {
                    // listen for next request
                    this.listener.BeginGetContext(new AsyncCallback(ProcessContextAsync), this.listener);
                }
                else
                {
                    logger.Send(new Message("Shutdown requested"));
                }
            }
            catch (ObjectDisposedException)
            {
                // Intentionally not doing anything with the exception.
            }
        }

        public async Task<int> RunAsync(string[] args)
        {
            if (running)
            {
                return 1;
            }

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(hostUrl);
                listener.Start();
            }
            catch (Exception e)
            {
                logger.Send(new Message("Failed to start server")
                    .With(e));
                return -1;
            }
            logger.Send($"Listening on {hostUrl}, content from {hostDir}");
            running = true;

            IAsyncResult ar = listener.BeginGetContext(new AsyncCallback(ProcessContextAsync), this.listener);

            while (running)
            {
                string cmd = Console.ReadLine();

                if (!running)
                {
                    break;
                }

                switch (cmd)
                {
                    case "quit":
                        running = false;
                        break;
                    default:
                        logger.Send("Invalid command");
                        break;
                }
            }

            listener.Abort();
            listener.Close();

            return 0;
        }
    }
}
