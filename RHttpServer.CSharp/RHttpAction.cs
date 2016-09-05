using System;
using System.Collections.Generic;
using RHttpServer.Request;
using RHttpServer.Response;

namespace RHttpServer
{
    internal class RHttpAction
    {
        internal RHttpAction(string route, Action<RRequest, RResponse> action)
        {
            RouteTree = route.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            RouteLength = RouteTree.Length;

            Action = action;
            for (var i = 0; i < RouteTree.Length; i++)
            {
                var s = RouteTree[i];
                if (!s.StartsWith(":")) continue;
                Params.Add(new KeyValuePair<int, string>(i, s.TrimStart(':')));
                RouteTree[i] = "^";
            }
        }

        internal readonly string[] RouteTree;

        internal List<KeyValuePair<int, string>> Params { get; } = new List<KeyValuePair<int, string>>();

        internal Action<RRequest, RResponse> Action { get; }
        internal int RouteLength { get; }

        internal bool HasRouteStep(int step, params string[] route)
        {
            if (step > RouteLength - 1) return false;
            var rs = RouteTree[step];
            foreach (var s in route)
            {
                if (s == rs) return true;
            }
            return false;
        }
    }
}