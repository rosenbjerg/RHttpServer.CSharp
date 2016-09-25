using System;
using ServiceStack;

namespace RHttpServer
{
    internal class RouteTreeManager
    {
        private readonly RouteTree _deleteTree = new RouteTree("", null);
        private readonly RouteTree _getTree = new RouteTree("", null);
        private readonly RouteTree _postTree = new RouteTree("", null);
        private readonly RouteTree _putTree = new RouteTree("", null);
        private readonly RouteTree _headTree = new RouteTree("", null);
        private readonly RouteTree _optionsTree = new RouteTree("", null);


        internal bool AddRoute(RHttpAction action, HttpMethod method)
        {
            switch (method)
            {
                case HttpMethod.GET:
                    return AddToTree(_getTree, action);
                case HttpMethod.POST:
                    return AddToTree(_postTree, action);
                case HttpMethod.PUT:
                    return AddToTree(_putTree, action);
                case HttpMethod.DELETE:
                    return AddToTree(_deleteTree, action);
                case HttpMethod.HEAD:
                    return AddToTree(_headTree, action);
                case HttpMethod.OPTIONS:
                    return AddToTree(_optionsTree, action);
            }
            return false;
        }

        private bool AddToTree(RouteTree tree, RHttpAction action)
        {
            var rTree = action.RouteTree;
            var len = rTree.Length;
            for (var i = 0; i < len; i++)
            {
                var ntree = tree.AddBranch(rTree[i]);
                tree = ntree;
            }
            if (tree.Action != null) throw new RHttpServerException("Cannot add two actions to the same route");
            tree.Action = action;
            return true;
        }

        internal RHttpAction SearchInTree(string route, HttpMethod meth, out bool generalFallback)
        {
            RouteTree tree = null, branch = null;
            switch (meth)
            {
                case HttpMethod.GET:
                    tree = _getTree;
                    break;
                case HttpMethod.POST:
                    tree = _postTree;
                    break;
                case HttpMethod.PUT:
                    tree = _putTree;
                    break;
                case HttpMethod.DELETE:
                    tree = _deleteTree;
                    break;
                case HttpMethod.HEAD:
                    tree = _headTree;
                    break;
                case HttpMethod.OPTIONS:
                    tree = _optionsTree;
                    break;
            }

            var split = route.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var len = split.Length;
            for (var i = 0; i < len; i++)
            {
                branch = tree.GetBranch(split[i]);
                if (branch != null) tree = branch;
                else break;
            }
            if ((branch == null || tree.Action == null) && len != 0)
            {
                while (branch == null || branch.Route != "*" || tree.Action == null)
                {
                    branch = tree;
                    if (branch.Stem == null) break;
                    tree = branch.Stem;
                }
                generalFallback = true;
                return tree?.General?.Action;
            }
            generalFallback = tree?.Route == "*";
            return tree?.Action;
        }
    }
}