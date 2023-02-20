using HttpServer.Main;
using HttpServer.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        protected string uploadPath = "upload.html";

        protected bool allowUpload = false;
        protected string uploadDirectory = "uploadbin";
        protected string uploadRequestRoute;

        /// <inheritdoc />
        public StaticFileServer(string hostUrl = null, string hostDir = null, bool allowUpload = false, ILogger logger = null)
            : base(hostUrl, hostDir, logger)
        {
            this.allowUpload = allowUpload;
        }

        /// <inheritdoc />
        protected override async Task Startup()
        {
            if (allowUpload)
            {
                // create upload directory
                Directory.CreateDirectory(uploadDirectory);
                Console.WriteLine($"Created directory {AbsolutePath(uploadDirectory)}");

                // amend upload request path if needed
                uploadRequestRoute = "/upload";
                int i = 0;
                while (Directory.Exists(uploadRequestRoute))
                {
                    uploadRequestRoute = $"/upload{i}";
                    ++i;
                }
                logger.Send($"Access the upload form at the route {uploadRequestRoute}");
            }
        }

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

            try
            {
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
                            return $"\n\t\t<ul><a href=\"{route}{relPath}\">{relPath}</a> <a href=\"{route}{relPath}\" download>Download</a></ul>";
                        }));

                    // send formatted file
                    await _SendFileAsync(GetTemplatePath(dirPath),
                        route, directoryList, fileList, allowUpload ? uploadRequestRoute : "");
                }
                else if (allowUpload && route == uploadRequestRoute)
                {
                    if (ctx.Request.HttpMethod == "POST")
                    {
                        // receive upload file
                        var bodyStream = ctx.Request.InputStream;
                        var filenames = ctx.Request.QueryString.GetValues("name");
                        var filename = filenames != null && filenames.Length > 0
                            ? filenames[0]
                            : $"upload-{DateTime.Now}";

                        // read as buffer
                        List<byte> bytes = new List<byte>();
                        byte[] buffer = new byte[1024];
                        int noBytes;
                        while ((noBytes = bodyStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            int diff = buffer.Length - noBytes;
                            bytes.AddRange(buffer);
                            if (diff > 0)
                            {
                                bytes.RemoveRange(bytes.Count - diff, diff);
                            }
                        }
                        var bodyBuffer = bytes.ToArray();

                        // write file to output directory
                        FileStream f = File.OpenWrite($"{uploadDirectory}/{filename}");
                        f.Write(bodyBuffer, 0, bodyBuffer.Length);
                        f.Close();

                        logger.Send(new Message("Downloaded file")
                            .With("length", bodyBuffer.Length)
                            .With("name", filename));
                    }
                    else
                    {
                        // return upload form
                        await _SendFileAsync(GetTemplatePath(uploadPath), uploadRequestRoute);
                    }
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
