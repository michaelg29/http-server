using HttpServer.Logging;
using HttpServer.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer
{
    public class ControllerEndpoint : Attribute
    {
        public string Route { get; set; }
        public HttpMethod Method { get; set; }

        public ControllerEndpoint(string method, string route)
        {
            Method = new HttpMethod(method);
            Route = route;
        }
    }

    public class MultipartFormData
    {
        public string ContentDisposition { get; set; }

        public string ContentType { get; set; }

        public string ContentLength { get; set; }

        public string Name { get; set; }

        public string FileName { get; set; }

        public byte[] Content { get; set; }

        internal Encoding encoding;

        public override string ToString()
        {
            return encoding.GetString(Content);
        }
    }


    /// <summary>
    /// Web server class
    /// </summary>
    public class WebServer : Main.HttpServer, Main.IConfigurationStore
    {
        private static readonly IList<Type> primitiveTypes = new List<Type>
        {
            typeof(decimal),
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid)
        };

        public RouteTree RouteTree { get; private set; }

        private IDictionary<string, object> variables;
        private IDictionary<Type, object> services;

        private Func<HttpListenerContext, Exception, Main.IConfigurationStore, object> ExceptionHandler;
        private Type ExceptionHandlerType;

        private Func<HttpListenerContext, Main.IConfigurationStore, object> NotFoundHandler;
        private Type NotFoundHandlerType;

        /// <summary>
        /// HTML template file paths
        /// </summary>
        protected string errorPath = "error.html";
        protected string notFoundPath = "notFound.html";

        /// <summary>
        /// Queue containing services to insert into dictionary
        /// Contains elements like (Interface type, implementation type, if re-added to queue)
        /// </summary>
        private Queue<(Type, Type, bool)> servicesToAdd;
        private Queue<Type> controllersToAdd;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="logger">Logger object.</param>
        public WebServer(WebServerConfig config = null, ILogger logger = null)
            : base(config.HostUrl, config.HostDir, logger)
        {
            RouteTree = new RouteTree();
            services = new Dictionary<Type, object>();
            services[typeof(ILogger)] = logger;
        }

        /// <inheritdoc />
        public void RegisterVariable<T>(string name, T value)
        {
            if (variables == null)
            {
                variables = new Dictionary<string, object>();
            }

            variables[name] = value;
        }

        /// <inheritdoc />
        public bool TryGetVariable<T>(string name, out T variable)
        {
            if (variables?.ContainsKey(name) ?? false)
            {
                object varObj = variables[name];
                if (varObj?.GetType().IsAssignableFrom(typeof(T)) ?? false)
                {
                    variable = (T)varObj;
                    return true;
                }
            }

            variable = default;
            return false;
        }

        /// <inheritdoc />
        public T GetVariable<T>(string name, T defaultVal = default)
        {
            return TryGetVariable(name, out T ret)
                ? ret
                : defaultVal;
        }

        /// <summary>
        /// Register a service to be used to instantiate controllers
        /// </summary>
        /// <typeparam name="T">Service interface type</typeparam>
        /// <typeparam name="U">Service implementation type</typeparam>
        public void RegisterService<T, U>() 
            where T : class
            where U : T
        {
            if (running)
            {
                throw new Exception("Server already running");
            }

            if (!typeof(T).IsInterface)
            {
                throw new Exception($"Type {typeof(T)} must be an interface type");
            }

            if (servicesToAdd == null)
            {
                servicesToAdd = new Queue<(Type, Type, bool)>();
            }

            servicesToAdd.Enqueue((typeof(T), typeof(U), false));
        }

        /// <summary>
        /// Register controller class with the web server to respond to requests
        /// </summary>
        /// <typeparam name="T">Type of controller</typeparam>
        public void RegisterController<T>() where T : Controller
        {
            if (running)
            {
                throw new Exception("Server already running");
            }

            if (controllersToAdd == null)
            {
                controllersToAdd = new Queue<Type>();
            }

            controllersToAdd.Enqueue(typeof(T));
        }

        /// <summary>
        /// Try to instantiate an object using services and parameters registered with the server
        /// </summary>
        /// <param name="type">Type of object to instantiate</param>
        /// <param name="instantiation">Instantiated object, null if unsuccessful</param>
        /// <returns>Whether the object was instantiated</returns>
        private bool TryInstantiateObject(Type type, out object instantiation)
        {
            // iterate over all constructors
            foreach (var constructor in type.GetConstructors())
            {
                // synthesize list of parameters
                IList<object> constructorParams = new List<object>();
                bool canCall = true;
                foreach (var param in constructor.GetParameters())
                {
                    // configuration variable
                    if (variables != null &&
                        variables.ContainsKey(param.Name) &&
                        param.ParameterType.IsAssignableFrom(variables[param.Name].GetType()))
                    {
                        constructorParams.Add(variables[param.Name]);
                        continue;
                    }

                    // otherwise must be an interface
                    if (!param.ParameterType.IsInterface)
                    {
                        throw new Exception("Service constructor parameter must be an interface type");
                    }

                    // check existing instantiations
                    if (services.ContainsKey(param.ParameterType))
                    {
                        constructorParams.Add(services[param.ParameterType]);
                        continue;
                    }

                    canCall = false;
                }

                if (canCall)
                {
                    instantiation = constructor.Invoke(constructorParams.ToArray());
                    return true;
                }
            }

            instantiation = null;
            return false;
        }

        /// <inheritdoc />
        protected override async Task Startup()
        {
            // register services
            while (servicesToAdd?.Count > 0)
            {
                var v = servicesToAdd.Dequeue();

                Type interfaceType = v.Item1;
                Type implementationType = v.Item2;

                bool instantiated = TryInstantiateObject(implementationType, out object instantiation);
                if (instantiated)
                {
                    services[interfaceType] = instantiation;
                }
                else
                {
                    if (v.Item3)
                    {
                        // do not re-add back
                        throw new Exception($"Could not instantiate service of type {implementationType} : {interfaceType}");
                    }

                    v.Item3 = true;
                    servicesToAdd.Enqueue(v);
                }
            }

            // register controllers
            while (controllersToAdd?.Count > 0)
            {
                // instantiate controller
                Type controllerType = controllersToAdd.Dequeue();
                if (TryInstantiateObject(controllerType, out object controllerObj))
                {
                    Controller controller = controllerObj as Controller;
                    controller._server = this;
                    RouteTree.RegisterCaller(controllerType, controller);
                    foreach (var method in controller.GetType().GetMethods())
                    {
                        var attr = method.GetCustomAttributes(typeof(ControllerEndpoint), true)
                            .Select(a => a as ControllerEndpoint)
                            .FirstOrDefault();
                        if (attr != null)
                        {
                            RouteTree.AddRoute(attr.Method, attr.Route, controllerType, method);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Could not instantiate controller with type {controllerType}");
                }
            }
        }

        public void SetExceptionHandler<T>(Func<HttpListenerContext, Exception, Main.IConfigurationStore, object> handler)
        {
            ExceptionHandler = handler;
            ExceptionHandlerType = typeof(T);
        }

        public void SetNotFoundHandler<T>(Func<HttpListenerContext, Main.IConfigurationStore, object> handler)
        {
            NotFoundHandler = handler;
            NotFoundHandlerType = typeof(T);
        }

        /// <summary>
        /// Custom exception handler.
        /// </summary>
        /// <param name="e">Exception</param>
        /// <param name="type">Type of object from handler</param>
        /// <returns>Object from handler, error view if handler not defined.</returns>
        private object HandleException(Exception e, out Type type)
        {
            if (ExceptionHandler == null)
            {
                type = typeof(View);
                return new View(GetTemplatePath(errorPath),
                    e.GetType().ToString(), e.Message);
            }
            else
            {
                type = ExceptionHandlerType;
                return ExceptionHandler(ctx, e, this);
            }
        }

        /// <summary>
        /// Custom not found handler.
        /// </summary>
        /// <param name="type">Type of object form handler</param>
        /// <returns>Object from handler, not found view if handler not defined.</returns>
        private object HandleNotFound(out Type type)
        {
            if (NotFoundHandler == null)
            {
                type = typeof(View);
                return new View(GetTemplatePath(notFoundPath));
            }
            else
            {
                type = NotFoundHandlerType;
                return NotFoundHandler(ctx, this);
            }
        }

        const string BOUNDARY_MARKER = "boundary=";
        public ICollection<MultipartFormData> FormDataContent { get; private set; }

        private static string GetHeader(List<byte> bytes, int length, int startIdx, Encoding encoding, byte[] terminator)
        {
            if (bytes.TryIndexOf(length, terminator, out int idx, startIdx))
            {
                return encoding.GetString(bytes.GetRange(startIdx, idx - startIdx).ToArray());
            }
            return null;
        }

        private static MultipartFormData ParseMultipartFormData(List<byte> bytes, int startIdx, int endIdx, Encoding encoding)
        {
            MultipartFormData data = new MultipartFormData
            {
                encoding = encoding
            };
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
            if (bytes.TryIndexOf(length, dispositionBytes, out headerStartIdx, startIdx))
            {
                data.ContentDisposition = GetHeader(bytes, length, headerStartIdx + dispositionBytes.Length, encoding, semicolonByte);
            }
            if (bytes.TryIndexOf(length, nameBytes, out headerStartIdx, startIdx))
            {
                data.Name = GetHeader(bytes, length, headerStartIdx + nameBytes.Length, encoding, quoteByte);
            }
            if (bytes.TryIndexOf(length, filenameBytes, out headerStartIdx, startIdx))
            {
                data.FileName = GetHeader(bytes, length, headerStartIdx + filenameBytes.Length, encoding, quoteByte);
            }
            if (bytes.TryIndexOf(length, contentTypeBytes, out headerStartIdx, startIdx))
            {
                data.ContentType = GetHeader(bytes, length, headerStartIdx + contentTypeBytes.Length, encoding, newLineByte);
            }

            // read body
            if (bytes.TryIndexOf(length, bodySepBytes, out int bodyStartIdx, startIdx))
            {
                data.Content = bytes.GetRange(bodyStartIdx + bodySepBytes.Length, endIdx - bodyStartIdx - bodySepBytes.Length - 2).ToArray();
            }

            return data;
        }

        private void ParseMultipartForm(Stream stream, string boundary, Encoding encoding)
        {
            FileStream file = File.OpenWrite("multipart-form");
            stream.CopyTo(file);
            file.Close();

            FormDataContent = new List<MultipartFormData>();

            boundary = "--" + boundary;
            byte[] boundaryBytes = encoding.GetBytes(boundary);
            int boundaryLength = boundary.Length;

            file = File.OpenRead("multipart-form");

            List<byte> bytes = new List<byte>();
            byte[] buffer = new byte[1024];
            int idx = 0;
            int startIdx = -1;
            while (file.Read(buffer, 0, buffer.Length) > 0)
            {
                bytes.AddRange(buffer);
                while ((idx = bytes.IndexOf(bytes.Count, boundaryBytes, Math.Max(startIdx, 0))) > -1)
                {
                    if (startIdx > -1)
                    {
                        // found end
                        FormDataContent.Add(ParseMultipartFormData(bytes, startIdx, idx, encoding));

                        // prepare to read next form element
                        bytes.RemoveRange(0, idx + boundaryLength);
                        startIdx = 0;
                    }
                    else
                    {
                        // found start
                        startIdx = idx + boundaryLength;
                    }
                }
            }
            bytes.Clear();
        }

        /// <summary>
        /// Process current request
        /// </summary>
        protected override async Task ProcessRequest()
        {
            (bool, object, Type) res = (false, null, null);
            object retObj = null;
            Type retType = null;

            FormDataContent = null;
            try
            {
                // read body stream
                var bodyStream = ctx.Request.InputStream;
                string bodyStr = null;
                IDictionary<string, string> formParams = null;

                if (bodyStream != null)
                {
                    // parse body stream
                    Console.WriteLine(ctx.Request.ContentType);
                    if (ctx.Request.ContentType?.Contains("multipart/form-data") == true)
                    {
                        // get separator
                        int idx = ctx.Request.ContentType.IndexOf(BOUNDARY_MARKER);
                        if (idx >= 0)
                        {
                            idx += BOUNDARY_MARKER.Length;
                        }
                        ParseMultipartForm(bodyStream, ctx.Request.ContentType.Substring(idx), Request.ContentEncoding ?? Encoding.UTF8);
                    }
                    else
                    {
                        var body = new StringBuilder();
                        byte[] buffer = new byte[1024];
                        int noBytes;
                        while ((noBytes = bodyStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            body = body.Append((Request.ContentEncoding ?? Encoding.UTF8).GetString(buffer, 0, noBytes));
                        }

                        Console.WriteLine(body.ToString());

                        if (ctx.Request.ContentType == "application/x-www-form-urlencoded")
                        {
                            formParams = body.ToString().ParseAsQuery();
                        }
                        else if (ctx.Request.ContentType?.Contains("text") == true)
                        {
                            bodyStr = body.ToString();
                        }
                    }
                }

                // call function with endpoint
                res = await RouteTree.TryNavigate(new HttpMethod(ctx.Request.HttpMethod),
                    ctx.Request.RawUrl, bodyStr, formParams);
                if (res.Item1)
                {
                    retObj = res.Item2;
                    retType = res.Item3;
                }
                else
                {
                    ResponseCode = HttpStatusCode.NotFound;
                    retObj = HandleNotFound(out retType);
                }
            }
            catch (HttpListenerException e)
            {
                // HTTP Exception, cannot send another file
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
                res.Item1 = false;
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                // Endpoint exception, send to user
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e.InnerException);
                retObj = HandleException(e.InnerException, out retType);
            }
            catch (Exception e)
            {
                // General exception, send to user
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
                retObj = HandleException(e, out retType);
            }

            if (!(retObj == null || retType == null))
            {
                // send returned content
                ResponseCode = HttpStatusCode.OK;
                if (retType.IsPrimitive || primitiveTypes.Contains(retType))
                {
                    await SendStringAsync(retObj.ToString(), ctx.Response.ContentType ?? "text");
                }
                else if (retType == typeof(View))
                {
                    View view = retObj as View;
                    // ensure correct path
                    if (!view.Filepath.Contains(":"))
                    {
                        view.Filepath = AbsolutePath(view.Filepath);
                    }
                    await SendStringAsync(view.Format(), view.MimeType);
                }
                else if (retType == typeof(HttpResponseMessage))
                {
                    HttpResponseMessage response = retObj as HttpResponseMessage;
                    await SendResponseAsync(response);
                }
                else
                {
                    // serialize complex object to JSON
                    await SendStringAsync(JsonConvert.SerializeObject(retType, retType, null), "text/json");
                }
            }
            else if (res.Item1)
            {
                ResponseCode = HttpStatusCode.NoContent;
            }
        }

        /// <inheritdoc />
        protected override async Task<string> GetCommand()
        {
            return await Task.FromResult(Console.ReadLine());
        }
    }
}
