using System.Net.Http;

namespace HttpServer.Main
{
    public static class Extensions
    {
        public static HttpMethod Patch { get; } = new HttpMethod("PATCH");
    }
}
