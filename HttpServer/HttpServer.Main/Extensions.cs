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

        public static string HttpGet { get; } = HttpMethod.Get.Method;
        public static string HttpPut { get; } = HttpMethod.Put.Method;
        public static string HttpPost { get; } = HttpMethod.Post.Method;
        public static string HttpDelete { get; } = HttpMethod.Delete.Method;
        public static string HttpHead { get; } = HttpMethod.Head.Method;
        public static string HttpOptions { get; } = HttpMethod.Options.Method;
        public static string HttpTrace { get; } = HttpMethod.Trace.Method;
        public static string HttpPatch { get; } = Patch.Method;
    }
}
