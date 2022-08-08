using System.Collections.Generic;
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

        public static bool TryIndexOf(this IList<byte> buffer, int len, byte[] target, out int idx, int startIdx = 0)
        {
            idx = buffer.IndexOf(len, target, startIdx);
            return idx != -1;
        }

        public static int IndexOf(this IList<byte> buffer, int len, byte[] target, int startIdx = 0)
        {
            int upperBound = len - target.Length + startIdx;
            for (int i = startIdx; i < upperBound; i++)
            {
                int skip = 0;
                bool canSkip = false;
                bool match = true;
                for (int j = 0; j < target.Length && match; j++)
                {
                    // do not re-check first characters that do not match the first byte in the target
                    if (canSkip && buffer[i + j] != target[0])
                    {
                        skip++;
                    }
                    else
                    {
                        canSkip = false;
                    }

                    if (j == 0)
                    {
                        canSkip = true;
                    }

                    match = buffer[i + j] == target[j];
                }

                if (match)
                {
                    return i;
                }

                i += skip;
            }

            return -1;
        }
    }
}
