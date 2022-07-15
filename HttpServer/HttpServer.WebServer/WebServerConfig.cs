namespace HttpServer.WebServer
{
    public class WebServerConfig
    {
        /// <summary>
        /// Url to host content from.
        /// </summary>
        public string HostUrl { get; set; }

        /// <summary>
        /// Directory to host files from. This is where the server will look for files.
        /// </summary>
        public string HostDir { get; set; }
    }
}
