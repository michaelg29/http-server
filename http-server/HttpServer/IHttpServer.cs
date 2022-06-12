using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    public interface IHttpServer
    {
        HttpStatusCode ResponseCode
        {
            set;
        }
        long ContentLength
        {
            set;
        }
        string ContentType
        {
            set;
        }
        Task SendFileAsync(string filePath, params string[] args);
        Task<int> RunAsync(string[] args);
    }
}
