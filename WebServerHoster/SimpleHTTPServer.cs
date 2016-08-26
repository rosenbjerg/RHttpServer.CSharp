using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WebServerHoster
{
    /// <summary>
    /// Represents a HTTP server that can be configured before starting
    /// </summary>
    public class SimpleHttpServer
    {
        /// <summary>
        /// The publicly available folder
        /// </summary>
        public string PublicDir { get; }

        /// <summary>
        /// The current port in use
        /// </summary>
        public int Port { get; }

        private readonly List<SimpleHttpAction> _getActions = new List<SimpleHttpAction>();

        private readonly List<SimpleHttpAction> _postActions = new List<SimpleHttpAction>();

        private readonly List<SimpleHttpAction> _putActions = new List<SimpleHttpAction>();

        private readonly List<SimpleHttpAction> _deleteActions = new List<SimpleHttpAction>();

        //private readonly IDictionary<string, Action<SimpleRequest, SimpleResponse>> _postActions
        //    = new Dictionary<string, Action<SimpleRequest, SimpleResponse>>();

        //private readonly IDictionary<string, Action<SimpleRequest, SimpleResponse>> _putActions
        //    = new Dictionary<string, Action<SimpleRequest, SimpleResponse>>();

        //private readonly IDictionary<string, Action<SimpleRequest, SimpleResponse>> _deleteActions 
        //    = new Dictionary<string, Action<SimpleRequest, SimpleResponse>>();
        
        private Thread _serverThread;
        private HttpListener _listener;

        /// <summary>
        /// Add action to handle GET requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Get(string route, Action<SimpleRequest, SimpleResponse> action)
        {
            AddToActionList(new SimpleHttpAction(route, action), _getActions);
        }

        /// <summary>
        /// Add action to handle POST requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Post(string route, Action<SimpleRequest, SimpleResponse> action)
        {
            AddToActionList(new SimpleHttpAction(route, action), _postActions);
        }

        /// <summary>
        /// Add action to handle PUT requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Put(string route, Action<SimpleRequest, SimpleResponse> action)
        {
            AddToActionList(new SimpleHttpAction(route, action), _putActions);
        }

        /// <summary>
        /// Add action to handle DELETE requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Delete(string route, Action<SimpleRequest, SimpleResponse> action)
        {
            AddToActionList(new SimpleHttpAction(route, action), _deleteActions);
        }

        /// <summary>
        /// Constructs and starts a server with given port and using the given path as public folder.
        /// Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="port">Port of the server.</param>
        public SimpleHttpServer(string path, int port)
        {
            PublicDir = path;
            Port = port;
        }

        /// <summary>
        /// Constructs and starts a server with automatically found port and using the given path as public folder.
        /// Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        public SimpleHttpServer(string path)
        {
            PublicDir = path;
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            Port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
        }

        /// <summary>
        /// Starts the server on a separate thread
        /// </summary>
        public void Start()
        {
            _serverThread = new Thread(Listen);
            _serverThread.Start();
            Console.WriteLine("Server is running on port " + Port);
        }

        /// <summary>
        /// Stops the server thread.
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + Port + "/");
            _listener.Start();
            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception)
                {

                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            string route = context.Request.Url.AbsolutePath;
            Console.WriteLine(route);
            
            var method = context.Request.HttpMethod.ToUpper();

            IEnumerable<SimpleHttpAction> dict;
            switch (method)
            {
                case "GET":
                    dict = _getActions;
                    break;
                case "POST":
                    dict = _postActions;
                    break;
                case "PUT":
                    dict = _putActions;
                    break;
                case "DELETE":
                    dict = _deleteActions;
                    break;
                default:
                    Console.WriteLine($"Invalid HTTP method: {method} from {context.Request.LocalEndPoint}");
                    return;
            }

            var act = FindRouteAction(dict, route);
            if (act != null)
            {
                act.Action(new SimpleRequest(context.Request, GetParams(act, route)), new SimpleResponse(context.Response));
            }
            else
            {
                var publicFile = Path.Combine(PublicDir, route.TrimStart('/'));
                if (!string.IsNullOrEmpty(PublicDir) && File.Exists(publicFile))
                {
                    new SimpleResponse(context.Response).SendFile(publicFile);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.OutputStream.Close();
                }
            }
        }

        private static RequestParams GetParams(SimpleHttpAction act, string route)
        {
            var dict = new Dictionary<string, string>();
            var rTree = route.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var rLen = rTree.Length;
            foreach (var keyValuePair in act.Params)
            {
                if (keyValuePair.Key < rLen) dict.Add(keyValuePair.Value, rTree[keyValuePair.Key]);
            }
            return new RequestParams(dict);
        }

        private static SimpleHttpAction FindRouteAction(IEnumerable<SimpleHttpAction> actions, string route)
        {
            var rTree = route.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var rSteps = rTree.Length;
            IEnumerable<SimpleHttpAction> list = actions.Where(t => t.RouteLength == rSteps);
            int routeStep = 0;
            do
            {
                var l = list.Where(s => s.HasRouteStep(rTree[routeStep], routeStep)).ToList();
                if (!l.Any()) l = list.Where(s => s.HasRouteStep("^", routeStep)).ToList();
                if (!l.Any()) l = list.Where(s => s.HasRouteStep("*", routeStep)).ToList();
                list = l;
                routeStep++;
            } while (routeStep < rSteps && list.Count() > 1);
            return list.FirstOrDefault();
        }

        private static void AddToActionList(SimpleHttpAction action, ICollection<SimpleHttpAction> list)
        {
            if (list.Any(t => t.RouteTree.SequenceEqual(action.RouteTree))) throw new SimpleHttpServerException("Cannot add two actions to the same route");
            list.Add(action);
        }
    }
}