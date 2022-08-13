using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace HttpServer.WebServer
{
    /// <summary>
    /// Extension methods for the IDictionary interface
    /// </summary>
    internal static class IDictionaryExtensions
    {
        /// <summary>
        /// Try and get a value from a possibly null dictionary
        /// </summary>
        /// <typeparam name="T">Key type</typeparam>
        /// <typeparam name="U">Value type</typeparam>
        /// <param name="dict">Possibly null dictionary</param>
        /// <param name="key">Key</param>
        /// <param name="val">Output value</param>
        /// <param name="defaultVal">Default value to assign to output</param>
        /// <returns>If the dictionary exists and the key was found</returns>
        public static bool TryGet<T, U>(IDictionary<T, U> dict, T key, out U val, U defaultVal = default)
        {
            if (key != null && (dict?.ContainsKey(key)).GetValueOrDefault())
            {
                val = dict[key];
                return true;
            }

            val = defaultVal;
            return false;
        }

        /// <summary>
        /// Parse a string as a query string with escaped characters
        /// </summary>
        /// <param name="str">The string to parse</param>
        /// <returns>The query parameters in dictionary form</returns>
        public static IDictionary<string, string> ParseAsQuery(this string str)
        {
            IDictionary<string, string> query = new Dictionary<string, string>();

            string[] paramArr = str.Split('&');
            foreach (string paramStr in paramArr)
            {
                string[] splitParam = paramStr.Split('=');
                if (splitParam.Length != 2)
                {
                    continue;
                }
                query[Uri.UnescapeDataString(splitParam[0])]
                    = Uri.UnescapeDataString(splitParam[1]);
            }

            return query;
        }
    }

    /// <summary>
    /// Class representing the route tree for the web server
    /// </summary>
    public class RouteTree
    {
        /// <summary>
        /// Private class to represent a method to call
        /// </summary>
        private class Method
        {
            /// <summary>
            /// Method information for the associated function
            /// </summary>
            public MethodInfo MethodInfo { get; set; }

            /// <summary>
            /// Type of class that calls this method
            /// </summary>
            public Type CallerType { get; set; }
        }

        /// <summary>
        /// Private node class for RouteTree
        /// </summary>
        private class RouteTreeNode
        {
            /// <summary>
            /// Variable name of the argument
            /// </summary>
            public string ArgName { get; set; }

            /// <summary>
            /// ArgType instance for the argument
            /// </summary>
            public ArgType ArgType { get; set; }

            /// <summary>
            /// Mapping of HttpMethods to actions
            /// </summary>
            private IDictionary<HttpMethod, Method> Functions { get; set; }

            /// <summary>
            /// Any subroutes not requiring an argument
            /// </summary>
            private IDictionary<string, RouteTreeNode> PlainSubRoutes { get; set; } // route => node

            /// <summary>
            /// Any subroutes requiring an argument
            /// </summary>
            private IDictionary<Type, RouteTreeNode> ArgSubRoutes { get; set; } // arg type => node

            /// <summary>
            /// Plain route constructor
            /// </summary>
            public RouteTreeNode() { }

            /// <summary>
            /// Argument route constructor
            /// </summary>
            /// <param name="argName">Name of the variable</param>
            /// <param name="argType">ArgType of the argument</param>
            public RouteTreeNode(string argName, ArgType argType)
            {
                ArgName = argName;
                ArgType = argType;
            }

            /// <summary>
            /// Set corresponding function for an HttpMethod
            /// </summary>
            /// <param name="key">HttpMethod</param>
            /// <param name="value">Function to associate</param>
            public Method this[HttpMethod key]
            {
                set
                {
                    if (Functions == null)
                    {
                        Functions = new Dictionary<HttpMethod, Method>();
                    }

                    Functions[key] = value;
                }
            }

            /// <summary>
            /// Obtain the endpoint action corresponding to the HttpMethod
            /// </summary>
            /// <param name="method">HttpMethod</param>
            /// <param name="function">Output function</param>
            /// <returns>If the action exists</returns>
            public bool TryGetFunction(HttpMethod method, out Method function)
                => IDictionaryExtensions.TryGet(Functions, method, out function);

            /// <summary>
            /// Set corresponding node for a subroute without an argument
            /// </summary>
            /// <param name="key">Subroute value</param>
            /// <param name="value">RouteTreeNode to associate</param>
            public RouteTreeNode this[string key]
            {
                set
                {
                    if (PlainSubRoutes == null)
                    {
                        PlainSubRoutes = new Dictionary<string, RouteTreeNode>();
                    }
                    PlainSubRoutes[key] = value;
                }
            }

            /// <summary>
            /// Obtain the non-argument node corresponding to the string value
            /// </summary>
            /// <param name="element">string value of the route element</param>
            /// <param name="rtn">Corresponding node</param>
            /// <returns>If the subroute exists</returns>
            public bool TryGetPlainSubRoute(string element, out RouteTreeNode rtn)
                => IDictionaryExtensions.TryGet(PlainSubRoutes, element, out rtn, this);

            /// <summary>
            /// Set corresponding node for a subroute with an argument
            /// </summary>
            /// <param name="key">Type of argument</param>
            /// <param name="value">RouteTreeNode to associate</param>
            public RouteTreeNode this[Type key]
            {
                set
                {
                    if (ArgSubRoutes == null)
                    {
                        ArgSubRoutes = new Dictionary<Type, RouteTreeNode>();
                    }
                    ArgSubRoutes[key] = value;
                }
            }

            /// <summary>
            /// Obtain the argument node corresponding to the argument type
            /// </summary>
            /// <param name="element">Argument type of the node</param>
            /// <param name="rtn">Corresponding node</param>
            /// <returns>If the subroute exists</returns>
            public bool TryGetArgSubRoute(Type argType, out RouteTreeNode rtn)
                => IDictionaryExtensions.TryGet(ArgSubRoutes, argType, out rtn, this);
        }

        /// <summary>
        /// Root node of the tree
        /// </summary>
        private RouteTreeNode root;

        /// <summary>
        /// Mapping between caller types and their instances
        /// </summary>
        private IDictionary<Type, object> callers;

        /// <summary>
        /// Register a caller in the tree
        /// </summary>
        /// <param name="callerType">Type of the caller</param>
        /// <param name="caller">Caller instance</param>
        public void RegisterCaller(Type callerType, object caller)
        {
            if (callers == null)
            {
                callers = new Dictionary<Type, object>();
            }
            callers[callerType] = caller;
        }

        /// <summary>
        /// Add a method to the route tree
        /// </summary>
        /// <param name="method">Associated HTTP method with the endpoint</param>
        /// <param name="route">Route to the endpoint</param>
        /// <param name="caller">Caller type for the endpoint</param>
        /// <param name="function">Function to be called for the endpoint</param>
        public void AddRoute(HttpMethod method, string route, Type caller, MethodInfo function)
        {
            if (root == null)
            {
                root = new RouteTreeNode();
            }

            // start at root and build path to target node
            var currentNode = root;
            foreach (var el in route.Split('/'))
            {
                // skip empty elements
                if (string.IsNullOrEmpty(el))
                {
                    continue;
                }

                RouteTreeNode nextNode = null;
                if (el.StartsWith("{") && el.EndsWith("}"))
                {
                    // argument node identified by: {name:type}
                    string typeStr = el.Substring(1, el.Length - 2);
                    // get name
                    int idx = typeStr.IndexOf(':');
                    string name = string.Empty;
                    if (idx == -1)
                    {
                        // default argument type is string
                        name = typeStr;
                        typeStr = "string";
                    }
                    else
                    {
                        name = typeStr.Substring(0, idx);
                        typeStr = typeStr.Substring(idx + 1);
                    }
                    // get type
                    ArgType argType = ArgType.GetArgType(typeStr);
                    nextNode = currentNode.TryGetArgSubRoute(argType.Type, out RouteTreeNode rtn)
                        ? rtn
                        : new RouteTreeNode(name, argType);
                    currentNode[argType.Type] = nextNode;
                }
                else
                {
                    // plain route
                    nextNode = currentNode.TryGetPlainSubRoute(el, out RouteTreeNode rtn)
                        ? rtn
                        : new RouteTreeNode();
                    currentNode[el] = nextNode;
                }
                // advance pointer
                currentNode = nextNode;
            }

            // set method in endpoint
            currentNode[method] = new Method
            {
                MethodInfo = function,
                CallerType = caller
            };
        }

        private void _AddRoute(HttpMethod method, string route, Delegate function)
        {
            if (root == null)
            {
                root = new RouteTreeNode();
            }

            // start at root and build path to target node
            var currentNode = root;
            foreach (var el in route.Split('/'))
            {
                if (string.IsNullOrEmpty(el))
                {
                    continue;
                }

                RouteTreeNode nextNode = null;
                if (el.StartsWith("{") && el.EndsWith("}"))
                {
                    // argument node identified by: {name:type}
                    string typeStr = el.Substring(1, el.Length - 2);
                    // get name
                    int idx = typeStr.IndexOf(':');
                    string name = string.Empty;
                    if (idx == -1)
                    {
                        name = typeStr;
                        typeStr = "string";
                    }
                    else
                    {
                        name = typeStr.Substring(0, idx);
                        typeStr = typeStr.Substring(idx + 1);
                    }
                    // get type
                    ArgType argType = ArgType.GetArgType(typeStr);
                    nextNode = currentNode.TryGetArgSubRoute(argType.Type, out RouteTreeNode rtn)
                        ? rtn
                        : new RouteTreeNode(name, argType);
                    currentNode[argType.Type] = nextNode;
                }
                else
                {
                    // plain route
                    nextNode = currentNode.TryGetPlainSubRoute(el, out RouteTreeNode rtn)
                        ? rtn
                        : new RouteTreeNode();
                    currentNode[el] = nextNode;
                }
                currentNode = nextNode;
            }

            // set method in endpoint
            currentNode[method] = new Method
            {
                CallerType = null,
                MethodInfo = function.GetMethodInfo()
            };
        }

        /// <summary>
        /// Call the action corresponding to a route and method
        /// </summary>
        /// <param name="method">The method</param>
        /// <param name="route">The route</param>
        /// <param name="body">The request body</param>
        /// <returns>(Whether the action was found, returned object, return type)</returns>
        public async Task<(bool, object, Type)> TryNavigate(HttpMethod method, string route, string body = null, byte[] bodyBuffer = null, IDictionary<string, string> extraParams = null)
        {
            if (root == null)
            {
                return (false, null, null);
            }

            // parse query parameters
            IDictionary<string, string> queryParams = null;
            int paramIdx = route.IndexOf('?');
            if (paramIdx != -1)
            {
                queryParams = route.Substring(paramIdx + 1).ParseAsQuery();
                route = route.Substring(0, paramIdx);
            }

            // insert extra parameters
            if (extraParams != null)
            {
                if (queryParams == null)
                {
                    queryParams = extraParams;
                }
                else
                {
                    foreach (var kvp in extraParams)
                    {
                        queryParams[kvp.Key] = kvp.Value;
                    }
                }
            }

            // null check in case of failure
            if (queryParams == null)
            {
                queryParams = new Dictionary<string, string>();
            }

            // parse route parameters while traversing the tree to find the action
            var currentNode = root;
            IDictionary<string, object> routeArgs = new Dictionary<string, object>();
            foreach (var routeElement in route.Split('/'))
            {
                if (string.IsNullOrEmpty(routeElement)) continue;

                if (currentNode.TryGetPlainSubRoute(routeElement, out currentNode))
                {
                    // move to next node
                    continue;
                }
                else if (ArgType.TryParse(routeElement, out object val, out Type type)
                    && currentNode.TryGetArgSubRoute(type, out currentNode))
                {
                    // set argument value in dictionary
                    routeArgs[currentNode.ArgName] = val;
                }
                else
                {
                    return (false, null, null);
                }
            }

            // find action corresponding to the method
            if (currentNode.TryGetFunction(method, out var function))
            {
                // place arguments in order specified by the method
                IList<object> argsList = new List<object>();
                foreach (var param in function.MethodInfo.GetParameters())
                {
                    if (param.ParameterType == typeof(byte[]))
                    {
                        argsList.Add(bodyBuffer);
                        continue;
                    }

                    if (routeArgs.TryGetValue(param.Name, out object value))
                    {
                        // found from route
                        argsList.Add(value);
                        continue;
                    }

                    // look in query or body
                    if (!queryParams.TryGetValue(param.Name, out string valueStr))
                    {
                        // if not in query, assign as body
                        valueStr = body;
                    }

                    if (!string.IsNullOrEmpty(valueStr))
                    {
                        // parse query or body value as argument
                        if (ArgType.TryParse(param.ParameterType, valueStr, out object objVal))
                        {
                            argsList.Add(objVal);
                            continue;
                        }

                        try
                        {
                            // try JSON deserializing value
                            argsList.Add(
                                JsonConvert.DeserializeObject(valueStr, param.ParameterType));
                            continue;
                        }
                        catch { }
                    }

                    argsList.Add(param.HasDefaultValue
                        ? param.DefaultValue
                        : null);
                }

                // call function
                if (!IDictionaryExtensions.TryGet(callers, function.CallerType, out object caller))
                {
                    return (false, null, null);
                }
                object ret = function.MethodInfo.Invoke(caller, argsList.ToArray());
                Type retType = function.MethodInfo.ReturnType;
                if (retType == typeof(Task))
                {
                    // no return from task
                    ret = null;
                    retType = null;
                }
                else if (retType.IsGenericType
                    && retType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    // await task completion
                    Task task = ret as Task;
                    await task.ConfigureAwait(false);

                    // get return from task
                    ret = task.GetType().GetProperty(nameof(Task<object>.Result)).GetValue(task);
                    retType = ret.GetType();
                }
                return (true, ret, retType);
            }

            return (false, null, null);
        }

        #region TEMPLATE ADD METHODS

        public void AddRoute<Tret>
            (HttpMethod method, string route, Func<Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, Tret>
            (HttpMethod method, string route, Func<T0, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, Tret>
            (HttpMethod method, string route, Func<T0, T1, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, Tret>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, Tret> function)
                => _AddRoute(method, route, function);

        public void AddRoute
            (HttpMethod method, string route, Func<Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0>
            (HttpMethod method, string route, Func<T0, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1>
            (HttpMethod method, string route, Func<T0, T1, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2>
            (HttpMethod method, string route, Func<T0, T1, T2, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
            (HttpMethod method, string route, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, Task> function)
                => _AddRoute(method, route, function);

        public void AddRoute
            (HttpMethod method, string route, Action action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0>
            (HttpMethod method, string route, Action<T0> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1>
            (HttpMethod method, string route, Action<T0, T1> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2>
            (HttpMethod method, string route, Action<T0, T1, T2> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3>
            (HttpMethod method, string route, Action<T0, T1, T2, T3> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
                => _AddRoute(method, route, action);

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
            (HttpMethod method, string route, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
                => _AddRoute(method, route, action);

        #endregion
    }
}
