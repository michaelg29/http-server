﻿using HttpServer.Logging;
using HttpServer.WebServer.Content;
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
using System.Text.RegularExpressions;

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
        private MultipartForm _FormData { get; set; }
        private Task<MultipartForm> FormDataTask { get; set; }

        public async Task<MultipartForm> GetMultipartForm()
        {
            if (_FormData != null)
            {
                return _FormData;
            }

            if (FormDataTask != null)
            {
                _FormData = await FormDataTask;
                FormDataTask = null;
                return _FormData;
            }

            return null;
        }

        public async Task<MultipartFormData> GetMultipartFormData(string name)
        {
            var form = await GetMultipartForm();
            return form.Data.Where(d => d.Name == name).FirstOrDefault();
        }

        public async Task<byte[]> GetMultipartFormBuffer(string name)
        {
            byte[] buffer = null;
            var form = await GetMultipartForm();

            var formData = form.Data.Where(d => d.Name == name).FirstOrDefault();
            if (formData == null)
            {
                return null;
            }

            FileStream file = File.OpenRead(form.DataFile);
            file.Seek(formData.StartIdx, SeekOrigin.Begin);
            buffer = new byte[formData.Length];
            await file.ReadAsync(buffer, 0, formData.Length);
            file.Close();

            return buffer;
        }

        public async Task<string> GetMultipartFormString(string name)
        {
            byte[] buffer = null;
            var form = await GetMultipartForm();

            var formData = form.Data.Where(d => d.Name == name).FirstOrDefault();
            if (formData == null)
            {
                return null;
            }

            FileStream file = File.OpenRead(form.DataFile);
            file.Seek(formData.StartIdx, SeekOrigin.Begin);
            buffer = new byte[formData.Length];
            await file.ReadAsync(buffer, 0, formData.Length);
            file.Close();

            return form.Encoding.GetString(buffer);
        }

        /// <summary>
        /// Process current request
        /// </summary>
        protected override async Task ProcessRequest()
        {
            (bool, object, Type) res = (false, null, null);
            object retObj = null;
            Type retType = null;

            ResponseCode = HttpStatusCode.OK;

            try
            {
                // read body stream
                var bodyStream = ctx.Request.InputStream;
                string bodyStr = null;
                byte[] bodyBuffer = null;
                IDictionary<string, string> formParams = null;

                if (bodyStream != null && !string.IsNullOrEmpty(ctx.Request.ContentType))
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

                        FormDataTask = MultipartForm.Read(ctx.Request.ContentType.Substring(idx), bodyStream, Request.ContentEncoding ?? Encoding.UTF8, "multipart");
                    }
                    else if (Mime.TextMimeTypes.Any(s => Regex.Match(ctx.Request.ContentType, s).Success))
                    {
                        // read as text
                        var body = new StringBuilder();
                        byte[] buffer = new byte[1024];
                        int noBytes;
                        while ((noBytes = bodyStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            body = body.Append((Request.ContentEncoding ?? Encoding.UTF8).GetString(buffer, 0, noBytes));
                        }

                        if (ctx.Request.ContentType == "application/x-www-form-urlencoded")
                        {
                            formParams = body.ToString().ParseAsQuery();
                        }
                        else
                        {
                            bodyStr = body.ToString();
                            bodyBuffer = (Request.ContentEncoding ?? Encoding.UTF8).GetBytes(body.ToString());
                        }
                    }
                    else
                    {
                        // read as buffer
                        List<byte> bytes = new List<byte>();
                        byte[] buffer = new byte[1024];
                        int noBytes;
                        while ((noBytes = bodyStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            int diff = buffer.Length - noBytes;
                            bytes.AddRange(buffer);
                            if (diff > 0)
                            {
                                bytes.RemoveRange(bytes.Count - diff, diff);
                            }
                        }
                        bodyBuffer = bytes.ToArray();
                    }
                }

                // call function with endpoint
                res = await RouteTree.TryNavigate(new HttpMethod(ctx.Request.HttpMethod),
                    ctx.Request.RawUrl, bodyStr, bodyBuffer, formParams);
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
            catch (System.Reflection.TargetParameterCountException e)
            {
                // could not find corresponding method
                ResponseCode = HttpStatusCode.NotFound;
                logger.Send(e);
                retObj = HandleNotFound(out retType);
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

            (await GetMultipartForm())?.Clear();
        }

        /// <inheritdoc />
        protected override async Task<string> GetCommand()
        {
            return await Task.FromResult(Console.ReadLine());
        }
    }
}
