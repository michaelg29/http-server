using HttpServer.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Main
{
    public abstract class HttpServer : IHttpServer
    {
        /// <summary>
        /// Url hosting from
        /// </summary>
        protected string hostUrl;

        /// <summary>
        /// Directory the server is fetching contents from
        /// </summary>
        protected string hostDir;

        /// <summary>
        /// Construct an absolute path for a route
        /// </summary>
        /// <param name="route">The route</param>
        /// <returns>The absolute path</returns>
        protected string AbsolutePath(string route)
        {
            return route.StartsWith("/")
                ? $"{hostDir}{route}"
                : $"{hostDir}/{route}";
        }

        /// <summary>
        /// Construct an absolute path for template files
        /// </summary>
        /// <param name="path">Template file path</param>
        /// <returns>The absolute path</returns>
        protected string GetTemplatePath(string path)
        {
            return $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/{path}";
        }

        /// <summary>
        /// If the server is running
        /// </summary>
        protected bool running = false;

        /// <summary>
        /// Listener object for the server
        /// </summary>
        protected HttpListener listener = null;

        /// <summary>
        /// Context of the current request and response
        /// </summary>
        protected HttpListenerContext ctx = null;

        /// <summary>
        /// Logger object
        /// </summary>
        protected ILogger logger = null;

        /// <inheritdoc />
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
        public HttpServer(string hostUrl = null, string hostDir = null, ILogger logger = null)
        {
            this.hostUrl = string.IsNullOrEmpty(hostUrl)
                ? "http://+:8080/"
                : hostUrl;
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
        protected async Task _SendFileAsync(string filePath, params string[] args)
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
        }

        /// <summary>
        /// Read file stream into response
        /// </summary>
        /// <param name="filePath">Absolute path of file</param>
        protected async Task ReadFileToResponseAsync(string filePath)
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
        protected async Task ReadFormattedFileToResponseAsync(string filePath, params string[] args)
        {
            await SendStringAsync(File.ReadAllText(filePath), Mime.GetMimeType(filePath), args);
        }

        /// <inheritdoc />
        public async Task SendStringAsync(string content, string contentType="text", params string[] args)
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
            if (!string.IsNullOrEmpty(contentType)) ContentType = contentType;
            Encoding = Encoding.UTF8;

            // send bytes
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            await ctx.Response.OutputStream.WriteAsync(buffer, 0, content.Length);
            await ctx.Response.OutputStream.FlushAsync();
        }

        /// <inheritdoc />
        public async Task SendResponseAsync(HttpResponseMessage responseMessage)
        {
            // set status code
            ResponseCode = responseMessage.StatusCode;

            // set headers
            foreach (var header in responseMessage.Headers)
            {
                ctx.Response.Headers.Add(header.Key, string.Join(",", header.Value));
            }

            // set content headers
            ContentLength = responseMessage.Content.Headers.ContentLength.GetValueOrDefault(0);
            ContentType = responseMessage.Content.Headers.ContentType.MediaType;
            if (responseMessage.Content.Headers.ContentEncoding?.Count > 0)
            {
                Encoding = Encoding.GetEncoding(responseMessage.Content.Headers.ContentEncoding.ElementAt(0));
            }

            // copy content
            var inputStream = await responseMessage.Content.ReadAsStreamAsync();
            await inputStream.CopyToAsync(ctx.Response.OutputStream);
        }

        /// <summary>
        /// Process current request
        /// </summary>
        protected abstract Task ProcessRequest();

        /// <summary>
        /// Receiver for a request
        /// </summary>
        /// <param name="result">Async result</param>
        protected async void ProcessContextAsync(IAsyncResult result)
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

        /// <summary>
        /// Obtains command from user
        /// To be overridden
        /// </summary>
        /// <returns>String command</returns>
        protected abstract Task<string> GetCommand();

        /// <summary>
        /// Startup method called after starting listener
        /// To be overridden
        /// </summary>
        protected abstract Task Startup();

        /// <summary>
        /// Run the HTTP server
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <returns>Return code: 1 if already running, -1 if exception, 0 if successful close</returns>
        public async Task<int> RunAsync()
        {
            if (running)
            {
                // do not start again
                return 1;
            }

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                logger.Send("No network available");
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            var ips = host
                .AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            logger.Send(new Message("Hosting addresses").With("List", string.Join(", ", ips)));

            try
            {
                // create and bind listener
                listener = new HttpListener();
                foreach (var url in hostUrl.Split(';'))
                {
                    if (!url.EndsWith("/"))
                    {
                        listener.Prefixes.Add(url + "/");
                    }
                    else
                    {
                        listener.Prefixes.Add(url);
                    }
                }
                listener.Start();

                // startup callback
                await Startup();
            }
            catch (Exception e)
            {
                logger.Send(new Message("Failed to start server")
                    .With(e));
                listener.Close();
                return -1;
            }
            logger.Send($"Listening on {hostUrl}, content from {hostDir}");
            running = true;

            // start receiving thread
            IAsyncResult ar = listener.BeginGetContext(new AsyncCallback(ProcessContextAsync), this.listener);

            // command loop
            while (running)
            {
                string cmd = await GetCommand();

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

        /// <inheritdoc />
        public void Shutdown()
        {
            running = false;
        }
    }
}
