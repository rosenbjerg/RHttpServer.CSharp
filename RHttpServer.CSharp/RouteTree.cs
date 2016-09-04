using System.Collections.Generic;

namespace RHttpServer
{
    internal class RouteTree
    {
        internal RouteTree(string route, RouteTree stem)
        {
            Stem = stem;
            Route = route;
        }

        internal RouteTree Stem { get; private set; }

        internal Dictionary<string, RouteTree> Specific { get; } = new Dictionary<string, RouteTree>();
        internal RouteTree Parameter { get; private set; }
        internal RouteTree General { get; private set; }

        internal string Route { get; set; }
        internal RHttpAction Action { get; set; }

        internal RouteTree GetBranch(string route)
        {
            RouteTree rt = null;
            Specific.TryGetValue(route, out rt);
            return rt ?? (Parameter ?? General);
        }

        internal RouteTree AddBranch(string route)
        {
            switch (route)
            {
                case "*":
                    return General ?? (General = new RouteTree(route, this));
                case "^":
                    return Parameter ?? (Parameter = new RouteTree(route, this));
                default:
                    RouteTree nr = null;
                    if (!Specific.TryGetValue(route, out nr))
                    {
                        nr = new RouteTree(route, this);
                        Specific.Add(route, nr);
                    }
                    return nr;
            }
        }
    }
}