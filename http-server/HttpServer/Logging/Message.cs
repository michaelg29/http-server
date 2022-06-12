using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    /// <summary>
    /// Extension methods pertaining to the message class
    /// </summary>
    public static class MessageExtensions
    {
        /// <summary>
        /// Create a new message
        /// </summary>
        /// <param name="str">Title of the message</param>
        /// <returns>The new message</returns>
        public static Message With(this string str)
        {
            return new Message(str);
        }
    }

    /// <summary>
    /// Message class
    /// </summary>
    public class Message
    {
        /// <summary>
        /// ToString method for encoding an object
        /// </summary>
        public static Func<object, string> StandardEncoder = obj => obj.ToString();

        /// <summary>
        /// Title of message
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Message headers
        /// </summary>
        private List<KeyValuePair<string, string>> headers;

        /// <summary>
        /// default constructor
        /// </summary>
        public Message() { }

        /// <summary>
        /// Constructor with title
        /// </summary>
        /// <param name="title">Message title</param>
        public Message(string title)
        {
            Title = title;
        }

        /// <summary>
        /// Constructor with exception
        /// </summary>
        /// <param name="e">Message exception</param>
        public Message(Exception e)
        {
            this.With(e);
        }

        /// <summary>
        /// Constructor with headers
        /// </summary>
        /// <param name="headers">Headers</param>
        public Message(List<KeyValuePair<string, string>> headers)
        {
            // copy all header values
            this.headers = new List<KeyValuePair<string, string>>();
            foreach (var header in headers)
            {
                this.headers.Add(new KeyValuePair<string, string>(header.Key, header.Value));
            }
        }

        /// <summary>
        /// Constructor with title and headers
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="headers">Headers</param>
        public Message(string title, List<KeyValuePair<string, string>> headers)
            : this(headers)
        {
            Title = title;
        }

        /// <summary>
        /// Concatenate string value
        /// </summary>
        /// <param name="msg">String value</param>
        /// <returns>Message object with new entry</returns>
        public Message With(string msg)
        {
            return this.With(null, msg);
        }

        /// <summary>
        /// Concatenate encoded object value
        /// </summary>
        /// <param name="obj">Object value</param>
        /// <param name="encoder">Custom encoder function, defaults to ToString</param>
        /// <returns>Message object with new entry</returns>
        public Message With(object obj, Func<object, string> encoder = null)
        {
            return this.With(null, obj, encoder);
        }

        /// <summary>
        /// Concatenate object value attached to tag
        /// </summary>
        /// <param name="header">Header tag</param>
        /// <param name="value">Object value</param>
        /// <param name="encoder">Custom encoder function, defaults to ToString</param>
        /// <returns>Message object with new entry</returns>
        public Message With(string header, object value, Func<object, string> encoder = null)
        {
            if (headers == null)
            {
                headers = new List<KeyValuePair<string, string>>();
            }

            // decode object
            headers.Add(new KeyValuePair<string, string>(
                header,
                encoder == null || value == null
                    ? value?.ToString()
                    : encoder(value)));
            
            return this;
        }

        /// <summary>
        /// Attach exception and inner exception to message
        /// </summary>
        /// <param name="e">Exception object</param>
        /// <returns>Message object with new entry</returns>
        public Message With(Exception e)
        {
            // add exception
            this.With(LogField.EXCEPTION_TYPE, e.GetType())
                .With(LogField.EXCEPTION_MSG, e.Message);

            if (e.InnerException != null)
            {
                // add inner exception
                this.With(LogField.INNER_EXCEPTION_TYPE, e.InnerException.GetType())
                    .With(LogField.INNER_EXCEPTION_TYPE, e.InnerException.Message);
            }

            return this;
        }

        /// <summary>
        /// Attach current date to message
        /// </summary>
        /// <returns>Message object with new entry</returns>
        public Message WithDate()
        {
            return this.With(LogField.CURRENT_DATE_UTC, DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (headers == null)
            {
                // no headers to attach
                return Title;
            }

            // add title and new line
            string title = string.IsNullOrEmpty(Title)
                ? string.Empty
                : Title + Environment.NewLine;
            
            // add header values
            return title + string.Join(Environment.NewLine, headers.Select(header =>
                string.IsNullOrEmpty(header.Key)
                    ? header.Value
                    : header.Key + ": " + header.Value
            ));
        }
    }
}
