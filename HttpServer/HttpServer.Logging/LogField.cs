﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Logging
{
    /// <summary>
    /// Static fields to be used as Message header keys
    /// </summary>
    public static class LogField
    {
        public const string EXCEPTION_TYPE = "exception_type";
        public const string EXCEPTION_MSG = "exception_msg";
        public const string INNER_EXCEPTION_TYPE = "inner_exception_type";
        public const string INNER_EXCEPTION_MSG = "inner_exception_msg";
        public const string CURRENT_DATE_UTC = "current_date_utc";
    }
}
