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
                if (PlainSubRoutes == null)
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
                    routeTreeNode = default;
                    return false;
                }
            }

            public void SetArgSubRoute(Type argType, RouteTreeNode routeTreeNode)
            {
                if (ArgSubRoutes == null)
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
                    routeTreeNode = default;
                    return false;
                }
            }
        }

        private RouteTreeNode root;

        public void AddRoute(string route, Action<WebServer, object[]> action)
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
    }
}
