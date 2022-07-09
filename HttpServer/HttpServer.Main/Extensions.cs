using System.Net.Http;

namespace HttpServer.Main
{
    /// <summary>
    /// Extensions needed for the HttpServer instances
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Patch HttpMethod
        /// </summary>
        public static HttpMethod Patch { get; } = new HttpMethod("PATCH");

        public const string HttpGet = "GET";
        public const string HttpPut = "PUT";
        public const string HttpPost = "POST";
        public const string HttpDelete = "DELETE";
        public const string HttpHead = "HEAD";
        public const string HttpOptions = "OPTIONS";
        public const string HttpTrace = "TRACE";
        public const string HttpPatch = "PATCH";
    }
}
