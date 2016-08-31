using System;
using System.Collections.Generic;
using RHttpServer.Core.Request;
using RHttpServer.Core.Response;

namespace RHttpServer.Core
{
    public class RHttpAction
    {
        internal RHttpAction(string route, Action<RRequest, RResponse> action)
        {
            RouteTree = route.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            RouteLength = RouteTree.Length;

            Action = action;
            for (int i = 0; i < RouteTree.Length; i++)
            {
                var s = RouteTree[i];
                if (!s.StartsWith(":")) continue;
                Params.Add(new KeyValuePair<int, string>(i, s.TrimStart(':')));
                RouteTree[i] = "^";
            }
        }

        public List<KeyValuePair<int, string>> Params { get; } = new List<KeyValuePair<int, string>>();

        public Action<RRequest, RResponse> Action { get; }
        public int RouteLength { get; }

        public readonly string[] RouteTree;

        public bool HasRouteStep(int step, params string[] route)
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