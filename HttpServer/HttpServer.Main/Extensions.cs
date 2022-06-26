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
    }
}
