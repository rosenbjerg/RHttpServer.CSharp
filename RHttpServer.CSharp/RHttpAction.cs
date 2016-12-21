using System;
using System.Collections.Generic;
using RHttpServer.Request;
using RHttpServer.Response;

namespace RHttpServer
{
    internal sealed class RHttpAction
    {
        internal RHttpAction(string route, Action<RRequest, RResponse> action)
        {
            RouteTree = route.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
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
    }
}