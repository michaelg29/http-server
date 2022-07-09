using HttpServer.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Web server class
    /// </summary>
    public class WebServer : Main.HttpServer
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

        /// <summary>
        /// Queue containing services to insert into dictionary
        /// Contains elements like (Interface type, implementation type, if re-added to queue)
        /// </summary>
        private Queue<(Type, Type, bool)> servicesToAdd;
        private Queue<Type> controllersToAdd;

        /// <inheritdoc />
        public WebServer(string hostUrl = null, string hostDir = null, ILogger logger = null)
            : base(hostUrl, hostDir, logger)
        {
            RouteTree = new RouteTree();
        }

        /// <summary>
        /// Register a configuration variable
        /// </summary>
        /// <param name="name">Name of variable</param>
        /// <param name="value">Value of variable</param>
        public void RegisterVariable(string name, object value)
        {
            if (variables == null)
            {
                variables = new Dictionary<string, object>();
            }

            variables[name] = value;
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
        public void RegisterController<T>()
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
            services = new Dictionary<Type, object>();
            services[typeof(IHttpServer)] = this;

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
                if (TryInstantiateObject(controllerType, out object controller))
                {
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
            }
        }

        /// <summary>
        /// Process current request
        /// </summary>
        protected override async Task ProcessRequest()
        {
            try
            {
                // read body stream
                var bodyStream = ctx.Request.InputStream;
                var body = new StringBuilder();
                byte[] buffer = new byte[1024];
                int noBytes;
                while ((noBytes = bodyStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    body = body.Append(Encoding.UTF8.GetString(buffer, 0, noBytes));
                }

                // call function with endpoint
                var res = await RouteTree.TryNavigate(new HttpMethod(ctx.Request.HttpMethod),
                    ctx.Request.RawUrl, body.ToString());
                object ret = res.Item2;
                Type type = res.Item3;
                if (res.Item1)
                {
                    if (!(ret == null || type == null))
                    {
                        // send returned content
                        ResponseCode = HttpStatusCode.OK;
                        if (type.IsPrimitive || primitiveTypes.Contains(type))
                        {
                            await SendStringAsync(ret.ToString());
                        }
                        else
                        {
                            // serialize complex object to JSON
                            await SendStringAsync(JsonConvert.SerializeObject(ret, type, null), "text/json");
                        }
                    }
                    else
                    {
                        // no content
                        ResponseCode = HttpStatusCode.NoContent;
                    }
                }
                else
                {
                    // send 404 file
                    ResponseCode = HttpStatusCode.NotFound;
                    await _SendFileAsync(GetTemplatePath(notFoundPath));
                }
            }
            catch (HttpListenerException e)
            {
                // HTTP Exception, cannot send another file
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
            }
            catch (Exception e)
            {
                // General exception, send to user
                ResponseCode = HttpStatusCode.InternalServerError;
                logger.Send(e);
                await ReadFormattedFileToResponseAsync(GetTemplatePath(errorPath),
                    e.GetType().ToString(), e.Message);
            }
        }

        /// <inheritdoc />
        protected override async Task<string> GetCommand()
        {
            return await Task.FromResult(Console.ReadLine());
        }
    }
}
