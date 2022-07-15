using HttpServer.Main;
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
    public class StaticFileServer : Main.HttpServer
    {
        /// <summary>
        /// HTML template file paths
        /// </summary>
        protected string errorPath = "error.html";
        protected string notFoundPath = "notFound.html";
        protected string dirPath = "directory.html";

        /// <inheritdoc />
        public StaticFileServer(string hostUrl = null, string hostDir = null, ILogger logger = null)
            : base(hostUrl, hostDir, logger) { }

        /// <inheritdoc />
        protected override async Task Startup() { }

        /// <summary>
        /// Process current request
        /// </summary>
        protected override async Task ProcessRequest()
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

            try
            {
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
            catch (Exception e)
            {
                // General exception, send to user
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
                await ReadFormattedFileToResponseAsync(GetTemplatePath(errorPath),
                e.GetType().ToString(), e.Message);
            }
        }

        /// <inheritdoc />
        protected override async Task<string> GetCommand()
        {
            return await Task.FromResult(Console.ReadLine());
        }
    }
}
