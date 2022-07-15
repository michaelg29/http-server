namespace HttpServer.WebServer
{
    public class WebServerConfig
    {
        /// <summary>
        /// Url to host content from.
        /// </summary>
        public string HostUrl { get; set; }

        /// <summary>
        /// Directory to host files from. This is where the server will look for the error files.
        /// </summary>
        public string HostDir { get; set; }

        /// <summary>
        /// Path to the general error template file.
        /// </summary>
        public string ErrorPath { get; set; } = "error.html";

        /// <summary>
        /// Path to the not found response template file.
        /// </summary>
        public string NotFoundPath { get; set; } = "notFound.html";
    }
}
