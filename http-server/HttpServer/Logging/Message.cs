using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    public static class MessageExtensions
    {
        public static Message With(this string str)
        {
            return new Message(str);
        }
    }

    public class Message
    {
        public static Func<object, string> StandardDecoder = obj => obj.ToString();

        public string Title { get; set; }
        private List<KeyValuePair<string, string>> headers;

        public Message() { }

        public Message(string title)
        {
            Title = title;
        }

        public Message(Exception e)
        {
            this.With(e);
        }

        public Message(List<KeyValuePair<string, string>> headers)
        {
            this.headers = new List<KeyValuePair<string, string>>();
            foreach (var header in headers)
            {
                this.headers.Add(new KeyValuePair<string, string>(header.Key, header.Value));
            }
        }

        public Message(string title, List<KeyValuePair<string, string>> headers)
            : this(headers)
        {
            Title = title;
        }

        public Message With(string msg)
        {
            return this.With(null, msg);
        }

        public Message With(object obj, Func<object, string> decoder = null)
        {
            return this.With(null, obj, decoder);
        }

        public Message With(string header, object value, Func<object, string> decoder = null)
        {
            if (headers == null)
            {
                headers = new List<KeyValuePair<string, string>>();
            }

            headers.Add(new KeyValuePair<string, string>(
                header,
                decoder == null || value == null
                    ? value?.ToString()
                    : decoder(value)));
            
            return this;
        }

        public Message With(Exception e)
        {
            Message ret = this.With(LogField.EXCEPTION_TYPE, e.GetType())
                .With(LogField.EXCEPTION_MSG, e.Message);

            if (e.InnerException != null)
            {
                ret = ret.With(LogField.INNER_EXCEPTION_TYPE, e.InnerException.GetType())
                    .With(LogField.INNER_EXCEPTION_TYPE, e.InnerException.Message);
            }

            return ret;
        }

        public Message WithDate()
        {
            return this.With(LogField.CURRENT_DATE_UTC, DateTime.UtcNow);
        }

        public override string ToString()
        {
            if (headers == null)
            {
                return Title;
            }

            string title = string.IsNullOrEmpty(Title)
                ? string.Empty
                : Title + Environment.NewLine;
            
            return title + string.Join(Environment.NewLine, headers.Select(header =>
                string.IsNullOrEmpty(header.Key)
                    ? header.Value
                    : header.Key + ": " + header.Value
            ));
        }
    }
}
