using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RHttpServer.Plugins;
using RHttpServer.Security;

namespace RHttpServer
{
    /// <summary>
    /// Represents a HTTP server that can be configured before starting
    /// </summary>
    public class SimpleHttpServer
    {
        private readonly List<SimpleHttpAction> _getActions = new List<SimpleHttpAction>();
        private readonly List<SimpleHttpAction> _postActions = new List<SimpleHttpAction>();
        private readonly List<SimpleHttpAction> _putActions = new List<SimpleHttpAction>();
        private readonly List<SimpleHttpAction> _deleteActions = new List<SimpleHttpAction>();

        // TODO Rework Security into SimplePlugin

        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private readonly ConcurrentQueue<HttpListenerContext> _queue;
        private readonly SimplePlugins _simplePlugins = new SimplePlugins();
        private IHttpSecurityHandler _securityHandler;

        /// <summary>
        /// The publicly available folder
        /// </summary>
        public string PublicDir { get; }

        /// <summary>
        /// The current port in use
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Whether the security is turned on.
        /// Set using SetSecuritySettings(..)
        /// </summary>
        public bool SecurityOn { get; private set; }
        
        public void AddPlugin<TPluginInterface, TPlugin>(TPlugin plugin)
            where TPlugin : SimplePlugin, TPluginInterface
        {
            plugin.SetPlugins(_simplePlugins);
            _simplePlugins.Add(typeof(TPluginInterface), plugin);
        }

        /// <summary>
        /// Add action to handle GET requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Get(string route, Action<SimpleRequest, SimpleResponse> action) => AddToActionList(new SimpleHttpAction(route, action), _getActions);

        /// <summary>
        /// Add action to handle POST requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Post(string route, Action<SimpleRequest, SimpleResponse> action) => AddToActionList(new SimpleHttpAction(route, action), _postActions);

        /// <summary>
        /// Add action to handle PUT requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Put(string route, Action<SimpleRequest, SimpleResponse> action) => AddToActionList(new SimpleHttpAction(route, action), _putActions);

        /// <summary>
        /// Add action to handle DELETE requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Delete(string route, Action<SimpleRequest, SimpleResponse> action) => AddToActionList(new SimpleHttpAction(route, action), _deleteActions);

        public void SetSecuritySettings(bool enabled, IHttpSecuritySettings nonDefaultSecuritySettings = null, IHttpSecurityHandler nonDefaultSecurityHandler = null)
        {
            if (_securityHandler == null && nonDefaultSecurityHandler == null)
            {
                _securityHandler = new SimpleHttpSecurityHandler();
            }
            SecurityOn = enabled;
            if (nonDefaultSecurityHandler != null)
            {
                _securityHandler = nonDefaultSecurityHandler;
            }
            if (nonDefaultSecuritySettings == null) nonDefaultSecuritySettings = new SimpleHttpSecuritySettings();
            _securityHandler.Settings = nonDefaultSecuritySettings;
            if (enabled) _securityHandler.Start();
            else _securityHandler.Stop();
        }


        /// <summary>
        /// Constructs and starts a server with given port and using the given path as public folder.
        /// Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="port">Port of the server.</param>
        /// <param name="requestHandlerThreads">The amount of threads to handle the incoming requests</param>
        public SimpleHttpServer(string path, int port, int requestHandlerThreads = 2)
        {
            if (path.StartsWith("./")) path = Path.Combine(Environment.CurrentDirectory, path.Replace("./", ""));
            PublicDir = path;
            Port = port;
            _workers = new Thread[requestHandlerThreads];
            _queue = new ConcurrentQueue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
        }

        /// <summary>
        /// Constructs and starts a server with automatically found port and using the given path as public folder.
        /// Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="requestHandlerThreads">The amount of threads to handle the incoming requests</param>
        public SimpleHttpServer(string path, int requestHandlerThreads = 2)
        {
            if (path.StartsWith("./")) path = path.Replace("./", Environment.CurrentDirectory);
            PublicDir = path;
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            Port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            if (path.StartsWith("./")) path = Path.Combine(Environment.CurrentDirectory, path.Replace("./", ""));
            PublicDir = path;
            _workers = new Thread[requestHandlerThreads];
            _queue = new ConcurrentQueue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
        }
        

        /// <summary>
        /// Starts the server on a separate thread
        /// </summary>
        public void Start(bool localOnly = false)
        {
            if (!_simplePlugins.IsRegistered<IJsonConverter>()) AddPlugin<IJsonConverter, SimpleNewtonsoftJsonConverter>(new SimpleNewtonsoftJsonConverter());
            if (!_simplePlugins.IsRegistered<IPageRenderer>()) AddPlugin<IPageRenderer, EcsPageRenderer>(new EcsPageRenderer());
            _listener.Prefixes.Add($"http://{(localOnly ? "localhost" : "*")}:{Port}/");
            _listener.Start();
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
            Console.WriteLine($"Server is listening on port {Port} {(localOnly ? "  -  local only" : "")}");
            Console.WriteLine($"Handling requests on {_workers.Length} thread{(_workers.Length == 1 ? "" : "s")}");
        }
        
        public void Dispose() { Stop(); }

        /// <summary>
        /// Stops the server thread and all worker threads.
        /// </summary>
        public void Stop()
        {
            _securityHandler?.Stop();
            _stop.Set();
            _listenerThread.Join();
            foreach (var worker in _workers)
                worker.Join();
            _listener.Stop();
        }

        public RenderParams CreateRenderParams()
        {
            var renderParams = new RenderParams {};
            renderParams.SetPlugins(_simplePlugins.Use<IPageRenderer>());
            return renderParams;
        }

        private void HandleRequests()
        {
            try
            {
                while (_listener.IsListening)
                {
                    var context = _listener.BeginGetContext(ContextReady, null);

                    if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                _queue.Enqueue(_listener.EndGetContext(ar));
                _ready.Set();
            }
        }

        private void Worker()
        {
            WaitHandle[] wait = { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                if (!_queue.TryDequeue(out context))
                {
                    _ready.Reset();
                    continue;
                }
                if (!SecurityOn || _securityHandler.HandleRequest(context.Request)) Process(context);
            }
        }
        

        private void Process(HttpListenerContext context)
        {
            string route = context.Request.Url.AbsolutePath;
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
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                    return;
            }

            bool publicFileExists = false;
            var publicFile = !string.IsNullOrEmpty(PublicDir) ? Path.Combine(PublicDir, route.TrimStart('/')) : "";
            var act = FindRouteAction(dict, route, publicFile, out publicFileExists);
            if (act != null)
            {
                act.Action(new SimpleRequest(context.Request, GetParams(act, route)), new SimpleResponse(context.Response, _simplePlugins));
            }
            else
            {
                if (publicFileExists) new SimpleResponse(context.Response, _simplePlugins).SendFile(publicFile);
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                }
            }
        }

        private static RequestParams GetParams(SimpleHttpAction act, string route)
        {
            var rTree = route.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var rLen = rTree.Length;
            var dict = new Dictionary<string, string>();
            foreach (var keyValuePair in act.Params)
            {
                if (keyValuePair.Key < rLen) dict.Add(keyValuePair.Value, rTree[keyValuePair.Key]);
            }
            return new RequestParams(dict);
        }

        private static SimpleHttpAction FindRouteAction(IEnumerable<SimpleHttpAction> actions, string route, string publicFile, out bool fileExists)
        {
            var rTree = route.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var rSteps = rTree.Length;
            IList<IList<SimpleHttpAction>> lister = new List<IList<SimpleHttpAction>>();
            int routeStep = 0;
            lister.Add(actions.Where(t => t.RouteLength <= rSteps && (rSteps == 0 || t.HasRouteStep(0, rTree[routeStep], "^", "*"))).ToList());
            if (!lister[0].Any())
            {
                fileExists = false;
                return null;
            }
            for (routeStep = 1; routeStep < rSteps; routeStep++)
            {
                lister.Add(lister[routeStep-1].Where(s => s.HasRouteStep(routeStep, rTree[routeStep], "^", "*")).ToList());
            }
            routeStep = routeStep - 1;
            IEnumerable<SimpleHttpAction> list = lister[routeStep];
            if (list.All(s => s.RouteTree.Any(z => z.EndsWith("*"))) && publicFile != "" && File.Exists(publicFile))
            {
                fileExists = true;
                return null;
            }
            while (!list.Any() && routeStep > 0)
            {
                routeStep = routeStep - 1;
                var step = routeStep;
                list = lister[routeStep].Where(s => s.HasRouteStep(step, "*"));
            }
            fileExists = false;
            return list.FirstOrDefault();
        }

        private static void AddToActionList(SimpleHttpAction action, List<SimpleHttpAction> list)
        {
            if (list.Any(t => t.RouteTree.SequenceEqual(action.RouteTree))) throw new SimpleHttpServerException("Cannot add two actions to the same route");
            list.Add(action);
        }
    }
}