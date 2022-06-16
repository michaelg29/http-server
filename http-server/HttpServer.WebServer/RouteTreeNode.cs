using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.WebServer
{
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

            public static RouteTreeNode NewRouteTreeNode<T>(Action<WebServer, object[]> action)
            {
                return new RouteTreeNode(ArgType.GetArgType(typeof(T)), action);
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

        public void AddRoute<T1>
            (string route, Action<WebServer, T1> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0]));

        public void AddRoute<T1, T2>
            (string route, Action<WebServer, T1, T2> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1]));

        public void AddRoute<T1, T2, T3>
            (string route, Action<WebServer, T1, T2, T3> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2]));

        public void AddRoute<T1, T2, T3, T4>
            (string route, Action<WebServer, T1, T2, T3, T4> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3]));

        public void AddRoute<T1, T2, T3, T4, T5>
            (string route, Action<WebServer, T1, T2, T3, T4, T5> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4]));

        public void AddRoute<T1, T2, T3, T4, T5, T6>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7, T8>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7, T8> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6], (T8)obj[7]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7, T8, T9>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6], (T8)obj[7], (T9)obj[8]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6], (T8)obj[7], (T9)obj[8], (T10)obj[9]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6], (T8)obj[7], (T9)obj[8], (T10)obj[9], (T11)obj[10]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6], (T8)obj[7], (T9)obj[8], (T10)obj[9], (T11)obj[10], (T12)obj[11]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6], (T8)obj[7], (T9)obj[8], (T10)obj[9], (T11)obj[10], (T12)obj[11], (T13)obj[12]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6], (T8)obj[7], (T9)obj[8], (T10)obj[9], (T11)obj[10], (T12)obj[11], (T13)obj[12], (T14)obj[13]));

        public void AddRoute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
            (string route, Action<WebServer, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
            => _AddRoute(route, (ws, obj)
                => action(ws, (T1)obj[0], (T2)obj[1], (T3)obj[2], (T4)obj[3], (T5)obj[4], (T6)obj[5], (T7)obj[6], (T8)obj[7], (T9)obj[8], (T10)obj[9], (T11)obj[10], (T12)obj[11], (T13)obj[12], (T14)obj[13], (T15)obj[14]));
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

            currentNode.ThisAction(ws, routeArgsList.ToArray());
            return true;
        }
    }
}
