using HttpServer;
using HttpServer.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.StaticFileServer
{
    /// <summary>
    /// Server class to host files from a directory
    /// </summary>
    public class StaticFileServer : IHttpServer
    {
        /// <summary>
        /// Url hosting from
        /// </summary>
        private string hostUrl;

        /// <summary>
        /// Directory the server is fetching contents from
        /// </summary>
        private string hostDir;

        /// <summary>
        /// Construct an absolute path for a route
        /// </summary>
        /// <param name="route">The route</param>
        /// <returns>The absolute path</returns>
        private string AbsolutePath(string route)
        {
            return route.StartsWith("/")
                ? $"{hostDir}{route}"
                : $"{hostDir}/{route}";
        }

        /// <summary>
        /// HTML template file paths
        /// </summary>
        private string errorPath = "error.html";
        private string notFoundPath = "notFound.html";
        private string dirPath = "directory.html";

        /// <summary>
        /// Construct an absolute path for template files
        /// </summary>
        /// <param name="path">Template file path</param>
        /// <returns>The absolute path</returns>
        private string GetTemplatePath(string path)
        {
            return $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/{path}";
        }

        /// <summary>
        /// If the server is running
        /// </summary>
        private bool running = false;

        /// <summary>
        /// Listener object for the server
        /// </summary>
        private HttpListener listener = null;

        /// <summary>
        /// Context of the current request and response
        /// </summary>
        private HttpListenerContext ctx = null;

        /// <summary>
        /// Logger object
        /// </summary>
        private ILogger logger = null;

        /// <summary>
        /// Get the current request object
        /// </summary>
        public HttpListenerRequest Request
        {
            get => ctx.Request;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <summary>
        /// Set the file corresponding MIME type
        /// </summary>
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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hostUrl">URL to host from</param>
        /// <param name="hostDir">Directory to fetch content from</param>
        /// <param name="logger">Logger instance, defaults to console logger</param>
        public StaticFileServer(string hostUrl = null, string hostDir = null, ILogger logger = null)
        {
            this.hostUrl = hostUrl ?? "http://+:8080";
            this.hostDir = string.IsNullOrEmpty(hostDir)
                ? Directory.GetCurrentDirectory()
                : hostDir.TrimEnd('/');
            this.logger = logger ?? Logger.ConsoleLogger;
        }

        /// <summary>
        /// Send file to the current response
        /// </summary>
        /// <param name="relFilePath">File path relative to the host directory</param>
        /// <param name="args">Formatting arguments</param>
        public async Task SendFileAsync(string relFilePath, params string[] args)
        {
            await _SendFileAsync(AbsolutePath(relFilePath), args);
        }

        /// <summary>
        /// Send file to the current response
        /// </summary>
        /// <param name="filePath">Absolute file path</param>
        /// <param name="args">Formatting arguments</param>
        private async Task _SendFileAsync(string filePath, params string[] args)
        {
            try
            {
                // determine if we need to format
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
                // HTTP Exception, cannot send another file
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
            }
            catch (Exception e)
            {
                // General exception, send to user
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
                await ReadFormattedFileToResponseAsync(GetTemplatePath(errorPath),
                    e.GetType().ToString(), e.Message);
            }
        }

        /// <summary>
        /// Read file stream into response
        /// </summary>
        /// <param name="filePath">Absolute path of file</param>
        private async Task ReadFileToResponseAsync(string filePath)
        {
            // flush existing stream
            await ctx.Response.OutputStream.FlushAsync();
            Stream input = new FileStream(filePath, FileMode.Open);

            try
            {
                // set header values
                ContentLength = input.Length;
                FileExt = filePath;
                Encoding = Encoding.UTF8;

                // read file into blocks and send sequentially
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
                // always close the stream
                input.Close();
            }
        }

        /// <summary>
        /// Read file into response with formatting
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task ReadFormattedFileToResponseAsync(string filePath, params string[] args)
        {
            await SendStringAsync(File.ReadAllText(filePath), Mime.GetMimeType(filePath), args);
        }

        /// <inheritdoc />
        public async Task SendStringAsync(string content, string contentType, params string[] args)
        {
            // flush existing stream
            await ctx.Response.OutputStream.FlushAsync();

            // read file contents
            if (args.Count() > 0)
            {
                try
                {
                    // apply formatting to the string
                    string formatted = string.Format(content, args);
                    content = formatted;
                }
                catch (Exception e)
                {
                    logger.Send(new Message("Failed to format file")
                        .With(e));
                }
            }

            // set header values
            ContentLength = content.Length;
            if (contentType != null) ContentType = contentType;
            Encoding = Encoding.UTF8;

            // send bytes
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            await ctx.Response.OutputStream.WriteAsync(buffer, 0, content.Length);
            await ctx.Response.OutputStream.FlushAsync();
        }

        /// <summary>
        /// Process current request
        /// </summary>
        private async Task ProcessRequest()
        {
            // store route locally
            string route = Uri.UnescapeDataString(ctx.Request.Url.AbsolutePath);
            if (route == "/shutdown")
            {
                // shutdown server
                running = false;
                ResponseCode = HttpStatusCode.NoContent;
                return;
            }

            // get absolute path for file access
            string absolutePath = AbsolutePath(route);
            if (absolutePath.Contains(".."))
            {
                absolutePath = null;
            }

            if (absolutePath != null && File.Exists(absolutePath))
            {
                // physical file exists
                await _SendFileAsync(absolutePath);
            }
            else if (absolutePath != null && Directory.Exists(absolutePath))
            {
                // list directory contents to user
                string directoryList = string.Empty;
                string fileList = string.Empty;

                if (!route.EndsWith("/"))
                {
                    route += "/";
                }

                // generate navigatable directory list
                directoryList = string.Join("", Directory.GetDirectories(absolutePath)
                    .Select(s =>
                    {
                        int idx = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
                        string relPath = s.Substring(idx + 1);
                        return $"\n\t\t<ul><a href=\"{route}{relPath}\">{relPath}</a></ul>";
                    }));
                if (route.Length > 1)
                {
                    // parent directory link
                    int n = route.Length - 2;
                    int idx = Math.Max(route.LastIndexOf('/', n), route.LastIndexOf('\\', n));
                    string parentPath = route.Substring(0, idx + 1);
                    directoryList += $"\n\t\t<ul><a href=\"{parentPath}\">..</a></ul>";
                }

                // generate file list
                fileList = string.Join("", Directory.GetFiles(absolutePath)
                    .Select(s =>
                    {
                        int idx = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
                        string relPath = s.Substring(idx + 1);
                        return $"\n\t\t<ul><a href=\"{route}{relPath}\">{relPath}</a></ul>";
                    }));

                // send formatted file
                await _SendFileAsync(GetTemplatePath(dirPath),
                    route, directoryList, fileList);
            }
            else
            {
                // send 404 file
                ResponseCode = HttpStatusCode.NotFound;
                await _SendFileAsync(GetTemplatePath(notFoundPath));
            }
        }

        /// <summary>
        /// Receiver for a request
        /// </summary>
        /// <param name="result">Async result</param>
        private async void ProcessContextAsync(IAsyncResult result)
        {
            try
            {
                // get context
                ctx = this.listener.EndGetContext(result);

                // log the request
                logger.Send(new Message("Request")
                    .With("Url", ctx.Request.Url)
                    .With("Method", ctx.Request.HttpMethod)
                    .With("Host", ctx.Request.UserHostName)
                    .With("Agent", ctx.Request.UserAgent));

                // process and send return
                await ProcessRequest();

                // log the response
                logger.Send(new Message("Response")
                    .With("Code", ctx.Response.StatusCode)
                    .With("Type", ctx.Response.ContentType ?? null)
                    .With("Length", ctx.Response.ContentLength64));

                // close the stream
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
                // do not start again
                return 1;
            }

            try
            {
                // create and bind listener
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

            // start receiving thread
            IAsyncResult ar = listener.BeginGetContext(new AsyncCallback(ProcessContextAsync), this.listener);

            // command loop
            while (running)
            {
                // TODO make generic way
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

            // close listener
            listener.Abort();
            listener.Close();

            return 0;
        }
    }
}
