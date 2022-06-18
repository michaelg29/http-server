using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.WebServer
{
    internal static class ArrayExtensions
    {
        internal static T Get<T>(this object[] arr, int idx)
        {
            return idx < arr.Length
                ? (T)arr[idx]
                : default;
        }
    }

    public class RouteTree
    {
        private class RouteTreeNode
        {
            public ArgType ArgType { get; set; }

            public Action<WebServer, object[]> ThisAction { get; set; }

            private bool HasPlainSubRoutes = false;
            private IDictionary<string, RouteTreeNode> PlainSubRoutes; // route => node
            private bool HasArgSubRoutes = false;
            private IDictionary<Type, RouteTreeNode> ArgSubRoutes; // arg type => node

            public RouteTreeNode() { }

            public RouteTreeNode(Action<WebServer, object[]> action)
            {
                this.ThisAction = action;
            }

            public RouteTreeNode(ArgType argType, Action<WebServer, object[]> action = null)
                : this(action)
            {
                this.ArgType = argType;
            }

            public void SetPlainSubRoute(string element, RouteTreeNode routeTreeNode)
            {
                if (!HasPlainSubRoutes)
                {
                    PlainSubRoutes = new Dictionary<string, RouteTreeNode>();
                }
                PlainSubRoutes[element] = routeTreeNode;

                HasPlainSubRoutes = true;
            }

            public bool TryGetPlainSubRoute(string element, out RouteTreeNode routeTreeNode)
            {
                try
                {
                    routeTreeNode = PlainSubRoutes[element];
                    return routeTreeNode != null;
                }
                catch (Exception)
                {
                    routeTreeNode = this;
                    return false;
                }
            }

            public void SetArgSubRoute(Type argType, RouteTreeNode routeTreeNode)
            {
                if (!HasArgSubRoutes)
                {
                    ArgSubRoutes = new Dictionary<Type, RouteTreeNode>();
                }
                ArgSubRoutes[argType] = routeTreeNode;

                HasArgSubRoutes = true;
            }

            public bool TryGetArgSubRoute(Type argType, out RouteTreeNode routeTreeNode)
            {
                try
                {
                    routeTreeNode = ArgSubRoutes[argType];
                    return routeTreeNode != null;
                }
                catch (Exception)
                {
                    routeTreeNode = this;
                    return false;
                }
            }
        }

        private RouteTreeNode root;

        #region Add route template methods
        public void AddRoute
            (string route, Action<WebServer> action)
            => _AddRoute(route, (ws, obj)
                => action(ws));

        public void AddRoute<T0>
            (string route, Action<WebServer, T0> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0)));

        public void AddRoute<T0, T1>
            (string route, Action<WebServer, T0, T1> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1)));

        public void AddRoute<T0, T1, T2>
            (string route, Action<WebServer, T0, T1, T2> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2)));

        public void AddRoute<T0, T1, T2, T3>
            (string route, Action<WebServer, T0, T1, T2, T3> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3)));

        public void AddRoute<T0, T1, T2, T3, T4>
            (string route, Action<WebServer, T0, T1, T2, T3, T4> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4)));

        public void AddRoute<T0, T1, T2, T3, T4, T5>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6, T7> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6), obj.Get<T7>(7)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6, T7, T8> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6), obj.Get<T7>(7), obj.Get<T8>(8)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6), obj.Get<T7>(7), obj.Get<T8>(8), obj.Get<T9>(9)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6), obj.Get<T7>(7), obj.Get<T8>(8), obj.Get<T9>(9), obj.Get<T10>(10)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6), obj.Get<T7>(7), obj.Get<T8>(8), obj.Get<T9>(9), obj.Get<T10>(10), obj.Get<T11>(11)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6), obj.Get<T7>(7), obj.Get<T8>(8), obj.Get<T9>(9), obj.Get<T10>(10), obj.Get<T11>(11), obj.Get<T12>(12)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6), obj.Get<T7>(7), obj.Get<T8>(8), obj.Get<T9>(9), obj.Get<T10>(10), obj.Get<T11>(11), obj.Get<T12>(12), obj.Get<T13>(13)));

        public void AddRoute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            (string route, Action<WebServer, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, obj.Get<T0>(0), obj.Get<T1>(1), obj.Get<T2>(2), obj.Get<T3>(3), obj.Get<T4>(4), obj.Get<T5>(5), obj.Get<T6>(6), obj.Get<T7>(7), obj.Get<T8>(8), obj.Get<T9>(9), obj.Get<T10>(10), obj.Get<T11>(11), obj.Get<T12>(12), obj.Get<T13>(13), obj.Get<T14>(14)));
        #endregion

        private void _AddRoute(string route, Action<WebServer, object[]> action)
        {
            if (root == null)
            {
                root = new RouteTreeNode(null);
            }

            var currentNode = root;
            var routeElements = route.Split('/');
            foreach (var el in routeElements)
            {
                if (string.IsNullOrEmpty(el))
                {
                    continue;
                }

                RouteTreeNode nextNode = null;
                if (el.StartsWith('{') && !el.StartsWith("{{") &&
                    el.EndsWith('}') && !el.EndsWith("}}"))
                {
                    // argument route
                    string typeStr = el.TrimEnd('}').TrimStart('{');
                    ArgType argType = ArgType.GetArgType(typeStr);
                    nextNode = currentNode.TryGetArgSubRoute(argType.Type, out RouteTreeNode rtn)
                        ? rtn
                        : new RouteTreeNode(argType);
                    currentNode.SetArgSubRoute(argType.Type, nextNode);
                }
                else
                {
                    // plain route
                    nextNode = currentNode.TryGetPlainSubRoute(el, out RouteTreeNode rtn)
                        ? rtn
                        : new RouteTreeNode();
                    currentNode.SetPlainSubRoute(el, nextNode);
                }
                currentNode = nextNode;
            }
            currentNode.ThisAction = action;
        }

        public bool Navigate(WebServer ws, string route)
        {
            if (root == null)
            {
                return false;
            }

            // get query parameters
            int paramStart = route.IndexOf('?') + 1;
            IDictionary<string, string> paramsMapping = null;
            if (paramStart > 0)
            {
                paramsMapping = new Dictionary<string, string>();
                string paramStr = route.Substring(paramStart);
                route = route.Substring(0, paramStart - 1); // get rid of parameters from route
                //Uri.UnescapeDataString(ctx.Request.Url.AbsolutePath);
                var paramsArray = paramStr.Split('&');
                foreach (var p in paramsArray)
                {
                    int split;
                    if (string.IsNullOrEmpty(p)
                        || (split = p.IndexOf('=')) == -1)
                    {
                        continue;
                    }

                    paramsMapping.Add(
                        Uri.UnescapeDataString(p.Substring(0, split)),
                        Uri.UnescapeDataString(p.Substring(split + 1)));
                }
            }

            var currentNode = root;
            var routeElements = route.Split('/');
            var routeArgsList = new List<object>();
            foreach (var el in routeElements)
            {
                if (string.IsNullOrEmpty(el)
                    || currentNode.TryGetPlainSubRoute(el, out currentNode))
                {
                    continue;
                }
                else if (ArgType.TryParse(el, out object val, out Type type)
                    && currentNode.TryGetArgSubRoute(type, out currentNode))
                {
                    routeArgsList.Add(val);
                }
                else
                {
                    return false;
                }
            }

            if (paramsMapping != null)
            {
                routeArgsList.Add(paramsMapping);
            }

            try
            {
                currentNode.ThisAction(ws, routeArgsList.ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
