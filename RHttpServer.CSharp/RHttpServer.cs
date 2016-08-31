using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using RHttpServer.Core.Plugins;
using RHttpServer.Core.Plugins.Default;
using RHttpServer.Core.Request;
using RHttpServer.Core.Response;

namespace RHttpServer.Core
{
    /// <summary>
    /// Represents a HTTP server that can be configured before starting
    /// </summary>
    public class RHttpServer : IDisposable
    {
        private readonly List<RHttpAction> _getActions = new List<RHttpAction>();
        private readonly List<RHttpAction> _postActions = new List<RHttpAction>();
        private readonly List<RHttpAction> _putActions = new List<RHttpAction>();
        private readonly List<RHttpAction> _deleteActions = new List<RHttpAction>();

        internal static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();


        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEventSlim _stop, _ready;
        private readonly ConcurrentQueue<HttpListenerContext> _queue;
        private readonly RPluginCollection _rPluginCollection = new RPluginCollection();
        private bool _defPluginsReady;
        private bool _securityOn;

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
        public bool SecurityOn
        {
            get { return _securityOn; }
            set
            {
                if (_securityOn == value) return;
                _securityOn = value;
                if (_securityOn)
                {
                    _rPluginCollection.Use<IHttpSecurityHandler>().Start();
                }
                else
                {
                    _rPluginCollection.Use<IHttpSecurityHandler>().Stop();
                }
            }
        }

        /// <summary>
        /// Register a plugin to be used in the server
        /// </summary>
        /// <typeparam name="TPluginInterface">The type the plugin implements</typeparam>
        /// <typeparam name="TPlugin">The type of the plugin instance</typeparam>
        /// <param name="plugin">The instance of the plugin that will be registered</param>
        public void AddPlugin<TPluginInterface, TPlugin>(TPlugin plugin)
            where TPlugin : RPlugin, TPluginInterface
        {
            plugin.SetPlugins(_rPluginCollection);
            _rPluginCollection.Add(typeof(TPluginInterface), plugin);
        }

        ///// <summary>
        ///// Returns the registered instance of a plugin
        ///// </summary>
        ///// <typeparam name="TPluginInterface">The type the plugin implements</typeparam>
        ///// <param name="key">The interface the plugin instance is registered to and implements</param>
        ///// <returns>The instance of the registered plugin</returns>
        //public TPluginInterface GetPlugin<TPluginInterface>() => _rPluginCollection.Use<TPluginInterface>();

        /// <summary>
        /// Add action to handle GET requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Get(string route, Action<RRequest, RResponse> action) => AddToActionList(new RHttpAction(route, action), _getActions);

        /// <summary>
        /// Add action to handle POST requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Post(string route, Action<RRequest, RResponse> action) => AddToActionList(new RHttpAction(route, action), _postActions);

        /// <summary>
        /// Add action to handle PUT requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Put(string route, Action<RRequest, RResponse> action) => AddToActionList(new RHttpAction(route, action), _putActions);

        /// <summary>
        /// Add action to handle DELETE requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Delete(string route, Action<RRequest, RResponse> action) => AddToActionList(new RHttpAction(route, action), _deleteActions);

        /// <summary>
        /// Constructs and starts a server with given port and using the given path as public folder.
        /// Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="port">Port of the server.</param>
        /// <param name="requestHandlerThreads">The amount of threads to handle the incoming requests</param>
        public RHttpServer(int port, int requestHandlerThreads = 2, string path = "")
        {
            if (requestHandlerThreads < 1)
            {
                requestHandlerThreads = 1;
#if DEBUG
                Console.WriteLine("Minimum 1 request-handler threads");
#endif
            }
            if (path.StartsWith("./")) path = Path.Combine(Environment.CurrentDirectory, path.Replace("./", ""));
            PublicDir = path;
            Port = port;
            _workers = new Thread[requestHandlerThreads];
            _queue = new ConcurrentQueue<HttpListenerContext>();
            _stop = new ManualResetEventSlim(false);
            _ready = new ManualResetEventSlim(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
        }

        /// <summary>
        /// Constructs and starts a server with automatically found port and using the given path as public folder.
        /// Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="requestHandlerThreads">The amount of threads to handle the incoming requests</param>
        public RHttpServer(int requestHandlerThreads = 2, string path = "")
        {
            if (requestHandlerThreads < 1) requestHandlerThreads = 1;
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
            _stop = new ManualResetEventSlim(false);
            _ready = new ManualResetEventSlim(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
        }
        

        /// <summary>
        /// Starts the server on a separate thread
        /// </summary>
        public void Start(bool localOnly = false)
        {

            InitializeDefaultPlugins();
            _listener.Prefixes.Add($"http://{(localOnly ? "localhost" : "+")}:{Port}/");
            _listener.Start();
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
            Console.WriteLine("RHttpServer v. {0} started", Version);
#if DEBUG
            if (localOnly) Console.WriteLine("Listening on localhost only");
#endif
        }
        
        public void Dispose() { Stop(); }

        /// <summary>
        /// Initializes any default plugin if no other plugin is registered to same interface
        /// </summary>
        public void InitializeDefaultPlugins(bool securityOn = false, SimpleHttpSecuritySettings simpleHttpSecuritySettings = null)
        {
            if (_defPluginsReady) return;
            if (!_rPluginCollection.IsRegistered<IJsonConverter>()) AddPlugin<IJsonConverter, ServiceStackJsonConverter>(new ServiceStackJsonConverter());
            if (!_rPluginCollection.IsRegistered<IPageRenderer>()) AddPlugin<IPageRenderer, EcsPageRenderer>(new EcsPageRenderer());
            if (!_rPluginCollection.IsRegistered<IHttpSecurityHandler>()) AddPlugin<IHttpSecurityHandler, SimpleHttpSecurityHandler>(new SimpleHttpSecurityHandler());
            _defPluginsReady = true;
            if (simpleHttpSecuritySettings == null) simpleHttpSecuritySettings = new SimpleHttpSecuritySettings();
            _rPluginCollection.Use<IHttpSecurityHandler>().Settings = simpleHttpSecuritySettings;
            SecurityOn = securityOn;
        }

        /// <summary>
        /// Stops the server thread and all worker threads.
        /// </summary>
        public void Stop()
        {
            _rPluginCollection.Use<IHttpSecurityHandler>().Stop();
            _stop.Set();
            _listenerThread.Join();
            foreach (var worker in _workers)
                worker.Join();
            _listener.Stop();
        }

        /// <summary>
        /// Method to get a new RenderParams object
        /// </summary>
        /// <returns>A new RenderParams instance with access</returns>
        public RenderParams CreateRenderParams()
        {
            var renderParams = new RenderParams();
            renderParams.SetRenderer(_rPluginCollection.Use<IPageRenderer>());
            return renderParams;
        }

        private void HandleRequests()
        {
            try
            {
                while (_listener.IsListening)
                {
                    var context = _listener.BeginGetContext(ContextReady, null);

                    if (0 == WaitHandle.WaitAny(new[] { _stop.WaitHandle, context.AsyncWaitHandle }))
                        return;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
#endif
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                _queue.Enqueue(_listener.EndGetContext(ar));
                _ready.Set();
            }
            catch (HttpListenerException ex)
            {
#if DEBUG
                Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
#endif
            }
        }

        private void Worker()
        {
            WaitHandle[] wait = { _ready.WaitHandle, _stop.WaitHandle };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                if (!_queue.TryDequeue(out context))
                {
                    _ready.Reset();
                    continue;
                }
                if (!SecurityOn || _rPluginCollection.Use<IHttpSecurityHandler>().HandleRequest(context.Request)) Process(context);
            }
        }
        

        private void Process(HttpListenerContext context)
        {
            string route = context.Request.Url.AbsolutePath;
            var method = context.Request.HttpMethod.ToUpper();

            IEnumerable<RHttpAction> dict;
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
#if DEBUG
                    Console.WriteLine($"Invalid HTTP method: {method} from {context.Request.LocalEndPoint}");
#endif
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                    return;
            }

            bool publicFileExists = false;
            var publicFile = !string.IsNullOrEmpty(PublicDir) ? Path.Combine(PublicDir, route.TrimStart('/')) : "";
            var act = FindRouteAction(dict, route, publicFile, out publicFileExists);
            if (act != null)
            {
                act.Action(new RRequest(context.Request, GetParams(act, route)), new RResponse(context.Response, _rPluginCollection));
            }
            else
            {
                if (publicFileExists) new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                }
            }
        }

        private static RequestParams GetParams(RHttpAction act, string route)
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

        private static RHttpAction FindRouteAction(IEnumerable<RHttpAction> actions, string route, string publicFile, out bool fileExists)
        {
            var rTree = route.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var rSteps = rTree.Length;
            IList<IEnumerable<RHttpAction>> lister = new List<IEnumerable<RHttpAction>>();
            int routeStep = 0;
            lister.Add(actions.Where(t => t.RouteLength <= rSteps && (rSteps == 0 || t.HasRouteStep(0, rTree[routeStep], "^", "*"))).ToList());
            if (!lister[0].Any())
            {
                fileExists = false;
                return null;
            }
            for (routeStep = 1; routeStep < rSteps; routeStep++)
            {
                var step = routeStep;
                lister.Add(lister[routeStep-1].Where(s => s.HasRouteStep(step, rTree[step], "^", "*")));
                if (!lister[routeStep].Any()) break;
            }
            IEnumerable<RHttpAction> list = lister[--routeStep];
            if (list.All(s => s.RouteTree.Contains("*")) && File.Exists(publicFile))
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

        private static void AddToActionList(RHttpAction action, List<RHttpAction> list)
        {
            if (list.Any(t => t.RouteTree.SequenceEqual(action.RouteTree))) throw new RHttpServerException("Cannot add two actions to the same route");
            list.Add(action);
        }
    }
}