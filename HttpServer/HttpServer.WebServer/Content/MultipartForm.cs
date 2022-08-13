using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer.Content
{
    public class ByteQueue
    {
        public byte[] Data { get; private set; }
        public int Size { get; private set; }
        public int Head { get; private set; }
        public int OccupiedSize { get; private set; }

        public ByteQueue(int size)
        {
            Size = size;
            Data = new byte[size];
        }

        /// <summary>
        /// Insert byte array into the queue
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <param name="offset">Where to start copying from</param>
        /// <param name="length">How many elements to copy</param>
        public void Insert(byte[] buffer, int offset, int length)
        {
            // only take last {size} bytes
            int size = Math.Min(length, Size);

            // first index to read from in the buffer
            offset = (offset + length - size) % buffer.Length;

            // first index to write to in the data array
            int firstIdx = (Head + length - size + Size) % Size;

            // copy array
            Array.Copy(buffer, offset, Data, firstIdx, Math.Min(size, Size - firstIdx));
            if (size > Size - firstIdx)
            {
                Array.Copy(buffer, offset + Size - firstIdx, Data, 0, size - (Size - firstIdx));
            }

            // update cursor
            Head = (Head + length) % Size;
        }

        /// <summary>
        /// Read from a stream into the queue
        /// </summary>
        /// <param name="stream">The stream</param>
        /// <param name="length">The number of bytes to read, max value is one length of the queue</param>
        /// <returns></returns>
        public int ReadStream(Stream stream, int length)
        {
            length = Math.Min(length, Size);

            int noBytesRead = stream.Read(Data, Head, Math.Min(length, Size - Head));
            if (length > Size - Head && noBytesRead > 0)
            {
                noBytesRead += stream.Read(Data, 0, length - (Size - Head));
            }

            // update cursor
            Head = (Head + length) % Size;

            OccupiedSize = Math.Min(OccupiedSize + noBytesRead, Size);
            return noBytesRead;
        }

        private int AbsIdx(int offsetIdx)
        {
            if (OccupiedSize < Data.Length)
            {
                return offsetIdx;
            }
            return (Head + offsetIdx) % Size;
        }

        /// <summary>
        /// Find target byte sequence in the queue
        /// </summary>
        /// <param name="target">Target byte sequence</param>
        /// <param name="startIdx">Offset to start looking at</param>
        /// <returns>Offset index of the target, -1 if not found</returns>
        public int IndexOf(byte[] target, int startIdx = 0, int endIdx = -1)
        {
            if (endIdx == -1)
            {
                endIdx = Data.Length;
            }
            int upperBound = endIdx - target.Length;
            for (int _i = startIdx; _i < upperBound; _i++)
            {
                int skip = 0;
                bool canSkip = false;
                bool match = true;
                for (int j = 0; j < target.Length && match; j++)
                {
                    int i = AbsIdx(_i + j);
                    // do not re-check first characters that do not match the first byte in the target
                    if (canSkip && Data[i] != target[0])
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

                    match = Data[i] == target[j];
                }

                if (match)
                {
                    return _i;
                }

                _i += skip;
            }

            return -1;
        }

        public bool TryIndexOf(byte[] target, out int idx, int startIdx = 0, int endIdx = -1)
        {
            idx = IndexOf(target, startIdx, endIdx);
            return idx > -1;
        }

        /// <summary>
        /// Get a portion of the queue
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <param name="length">How many bytes to copy</param>
        /// <returns>The slice of the queue</returns>
        public byte[] Slice(int offset, int length)
        {
            length = Math.Min(length, Size);
            byte[] buffer = new byte[length];
            int abs = AbsIdx(offset);
            Array.Copy(Data, abs, buffer, 0, Math.Min(length, Size - abs));
            if (length > Size - abs)
            {
                Array.Copy(Data, 0, buffer, Size - abs, length - Size + abs);
            }
            return buffer;
        }

        public override string ToString()
        {
            return $"Head: {Head}, Data: {{{string.Join(", ", Data)}}}";
        }
    }

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
        public byte[] Boundary { get; private set; }

        public string DataFile { get; private set; }

        public Encoding Encoding { get; private set; }

        public List<MultipartFormData> Data { get; private set; }

        private static string GetHeader(ByteQueue bytes, int length, int startIdx, Encoding encoding, byte[] terminator)
        {
            if (bytes.TryIndexOf(terminator, out int idx, startIdx))
            {
                return encoding.GetString(bytes.Slice(startIdx, idx - startIdx));
            }
            return null;
        }

        private static void ParseMultipartFormData(ByteQueue bytes, int startIdx, int endIdx, Encoding encoding, MultipartFormData output)
        {
            int length = endIdx - startIdx;

            byte[] dispositionBytes = encoding.GetBytes("Content-Disposition: ");
            byte[] nameBytes = encoding.GetBytes("name=\"");
            byte[] filenameBytes = encoding.GetBytes("filename=\"");
            byte[] contentTypeBytes = encoding.GetBytes("Content-Type: ");
            byte[] quoteByte = encoding.GetBytes("\"");
            byte[] semicolonByte = encoding.GetBytes(";");
            byte[] newLineByte = encoding.GetBytes("\n");
            byte[] bodySepBytes = encoding.GetBytes("\r\n\r\n");

            // read headers
            int headerStartIdx = 0;
            if (bytes.TryIndexOf(dispositionBytes, out headerStartIdx, startIdx, endIdx))
            {
                output.ContentDisposition = GetHeader(bytes, length, headerStartIdx + dispositionBytes.Length, encoding, semicolonByte);
            }
            if (bytes.TryIndexOf(nameBytes, out headerStartIdx, startIdx, endIdx))
            {
                output.Name = GetHeader(bytes, length, headerStartIdx + nameBytes.Length, encoding, quoteByte);
            }
            if (bytes.TryIndexOf(filenameBytes, out headerStartIdx, startIdx, endIdx))
            {
                output.FileName = GetHeader(bytes, length, headerStartIdx + filenameBytes.Length, encoding, quoteByte);
            }
            if (bytes.TryIndexOf(contentTypeBytes, out headerStartIdx, startIdx, endIdx))
            {
                output.ContentType = GetHeader(bytes, length, headerStartIdx + contentTypeBytes.Length, encoding, newLineByte);
            }

            // read body
            if (bytes.TryIndexOf(bodySepBytes, out int bodyStartIdx, startIdx, endIdx))
            {
                output.StartIdx += bodyStartIdx + bodySepBytes.Length;
                output.Length = endIdx - (bodyStartIdx + bodySepBytes.Length);
            }
        }

        public static async Task<MultipartForm> Read(string boundary, Stream stream, Encoding encoding, string fileName)
        {
            boundary = "--" + boundary;
            byte[] boundaryBytes = encoding.GetBytes(boundary);
            MultipartForm multipartForm = new MultipartForm
            {
                Boundary = boundaryBytes,
                DataFile = fileName,
                Encoding = encoding,
                Data = new List<MultipartFormData>()
            };

            int boundaryLength = boundary.Length;

            // copy to file for persistence
            FileStream file = File.OpenWrite(fileName);
            stream.CopyTo(file);
            file.Close();

            file = File.OpenRead(fileName);

            // read stream in 1/2 kb increments
            int length = 1 << 9;
            // store consecutive blocks to read edges
            ByteQueue queue = new ByteQueue(length << 1);

            int idx;
            int noBytes;
            int startIdx = -1;
            int fileCursor = 0;
            MultipartFormData curData = null;
            while ((noBytes = queue.ReadStream(file, length)) > 0)
            {
                while ((idx = queue.IndexOf(boundaryBytes, Math.Max(startIdx, 0))) > -1)
                {
                    if (curData != null)
                    {
                        // read into element
                        if (curData.Length == 0)
                        {
                            // have not yet parsed
                            ParseMultipartFormData(queue, startIdx, idx > -1 ? idx : queue.OccupiedSize, encoding, curData);
                        }
                        else
                        {
                            // found end
                            curData.Length = fileCursor + (idx % length) - curData.StartIdx;
                        }

                        curData.Length -= 2;
                        multipartForm.Data.Add(curData);
                    }

                    // found start
                    startIdx = idx + boundaryLength;
                    curData = new MultipartFormData
                    {
                        StartIdx = fileCursor > (length << 1) ? fileCursor : 0
                    };
                }
                if (curData != null)
                {
                    if (curData.Length == 0)
                    {
                        // read into element
                        ParseMultipartFormData(queue, startIdx, queue.OccupiedSize, encoding, curData);
                    }
                    else
                    {
                        // increase body size
                        curData.ContentLength += length - idx;
                    }
                }

                fileCursor += noBytes;
                startIdx = 512;
            }
            file.Close();
            return multipartForm;
        }
        
        /// <summary>
        /// Clear list and overwrite file with random data
        /// </summary>
        public void Clear()
        {
            Boundary = null;
            Data.Clear();

            // get random string to overwrite file
            Random rnd = new Random();
            byte[] randBuffer = new byte[1024];
            rnd.NextBytes(randBuffer);

            // write to file
            FileStream file = File.OpenWrite(DataFile);
            file.Write(randBuffer, 0, 1024);
            file.Close();

            // delete file
            File.Delete(DataFile);
        }
    }
}
