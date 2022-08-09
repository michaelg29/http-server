using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer.Content
{
    public class MultipartFormData
    {
        public string ContentDisposition { get; set; }

        public string ContentType { get; set; }

        public string ContentLength { get; set; }

        public string Name { get; set; }

        public string FileName { get; set; }

        public int StartIdx { get; internal set; }

        public int Length { get; internal set; }
    }

    public class MultipartForm
    {
        public string DataFile { get; }

        public Encoding Encoding { get; set; }

        public List<MultipartFormData> Data { get; private set; }

        public static MultipartForm Read(Stream stream, string fileName)
        {
            // copy to file for persistence
            FileStream file = File.OpenWrite(fileName);
            stream.CopyTo(file);
            file.Close();
            return null;
        }
    }
}
