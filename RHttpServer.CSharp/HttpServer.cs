using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using RHttpServer.Plugins;
using RHttpServer.Plugins.Default;
using RHttpServer.Request;
using RHttpServer.Response;

namespace RHttpServer
{
    /// <summary>
    ///     Represents a HTTP server that can be configured before starting
    /// </summary>
    public class HttpServer : IDisposable
    {
        internal static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();


        /// <summary>
        ///     Constructs and starts a server with given port and using the given path as public folder.
        ///     Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="port">Port of the server.</param>
        /// <param name="requestHandlerThreads">The amount of threads to handle the incoming requests</param>
        public HttpServer(int port, int requestHandlerThreads = 2, string path = "")
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
        ///     Constructs and starts a server with automatically found port and using the given path as public folder.
        ///     Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="requestHandlerThreads">The amount of threads to handle the incoming requests</param>
        public HttpServer(int requestHandlerThreads = 2, string path = "")
        {
            if (requestHandlerThreads < 1) requestHandlerThreads = 1;
            if (path.StartsWith("./")) path = path.Replace("./", Environment.CurrentDirectory);
            PublicDir = path;
            //get an empty port
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            Port = ((IPEndPoint) l.LocalEndpoint).Port;
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


        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly ConcurrentQueue<HttpListenerContext> _queue;
        private readonly RPluginCollection _rPluginCollection = new RPluginCollection();
        private readonly RouteTreeManager _rtman = new RouteTreeManager();
        private readonly ManualResetEventSlim _stop, _ready;
        private readonly Thread[] _workers;
        private bool _defPluginsReady;
        private bool _securityOn;

        /// <summary>
        ///     The publicly available folder
        /// </summary>
        public string PublicDir { get; }

        /// <summary>
        ///     The current port in use
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Whether the security is turned on.
        ///     Set using SetSecuritySettings(..)
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
        ///     Register a plugin to be used in the server
        /// </summary>
        /// <typeparam name="TPluginInterface">The type the plugin implements</typeparam>
        /// <typeparam name="TPlugin">The type of the plugin instance</typeparam>
        /// <param name="plugin">The instance of the plugin that will be registered</param>
        public void RegisterPlugin<TPluginInterface, TPlugin>(TPlugin plugin)
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
        ///     Add action to handle GET requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Get(string route, Action<RRequest, RResponse> action)
            => _rtman.AddRoute(new RHttpAction(route, action), HttpMethod.GET);

        /// <summary>
        ///     Add action to handle POST requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Post(string route, Action<RRequest, RResponse> action)
            => _rtman.AddRoute(new RHttpAction(route, action), HttpMethod.POST);

        /// <summary>
        ///     Add action to handle PUT requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Put(string route, Action<RRequest, RResponse> action)
            => _rtman.AddRoute(new RHttpAction(route, action), HttpMethod.PUT);

        /// <summary>
        ///     Add action to handle DELETE requests to a given route
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Delete(string route, Action<RRequest, RResponse> action)
            => _rtman.AddRoute(new RHttpAction(route, action), HttpMethod.DELETE);


        /// <summary>
        ///     Starts the server on a separate thread
        /// </summary>
        public void Start(bool localOnly = false)
        {
            InitializeDefaultPlugins();
            _listener.Prefixes.Add($"http://{(localOnly ? "localhost" : "+")}:{Port}/");
            _listener.Start();
            _listenerThread.Start();

            for (var i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
            Console.WriteLine("RHttpServer v. {0} started", Version);
#if DEBUG
            if (localOnly) Console.WriteLine("Listening on localhost only");
#endif
        }

        /// <summary>
        ///     Initializes any default plugin if no other plugin is registered to same interface
        /// </summary>
        public void InitializeDefaultPlugins(bool renderCaching = true, bool securityOn = false,
            SimpleHttpSecuritySettings securitySettings = null)
        {
            if (_defPluginsReady) return;
            if (!_rPluginCollection.IsRegistered<IJsonConverter>())
                RegisterPlugin<IJsonConverter, ServiceStackJsonConverter>(new ServiceStackJsonConverter());
            if (!_rPluginCollection.IsRegistered<IPageRenderer>())
                RegisterPlugin<IPageRenderer, EcsPageRenderer>(new EcsPageRenderer());
            if (!_rPluginCollection.IsRegistered<IHttpSecurityHandler>())
                RegisterPlugin<IHttpSecurityHandler, SimpleServerProtection>(new SimpleServerProtection());
            _defPluginsReady = true;
            if (securitySettings == null) securitySettings = new SimpleHttpSecuritySettings();
            _rPluginCollection.Use<IHttpSecurityHandler>().Settings = securitySettings;
            _rPluginCollection.Use<IPageRenderer>().CachePages = renderCaching;
            SecurityOn = securityOn;
        }

        /// <summary>
        ///     Stops the server thread and all worker threads.
        /// </summary>
        public void Stop()
        {
            _rPluginCollection.Use<IHttpSecurityHandler>().Stop();
            _stop.Set();
            _listenerThread.Join();
            foreach (var worker in _workers)
                worker.Join(100);
            _listener.Stop();
        }

        /// <summary>
        ///     Method to get a new RenderParams object
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

                    if (0 == WaitHandle.WaitAny(new[] {_stop.WaitHandle, context.AsyncWaitHandle}))
                        return;
                }
            }
            catch (Exception)
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
            catch (Exception)
            {
#if DEBUG
                Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
#endif
            }
        }

        private void Worker()
        {
            WaitHandle[] wait = {_ready.WaitHandle, _stop.WaitHandle};
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                if (!_queue.TryDequeue(out context))
                {
                    _ready.Reset();
                    continue;
                }
                try
                {
                    if (!SecurityOn || _rPluginCollection.Use<IHttpSecurityHandler>().HandleRequest(context.Request))
                        Process(context);
                }
                catch (Exception)
                {
#if DEBUG
                    Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
#endif
                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            var route = context.Request.Url.AbsolutePath;
            var method = context.Request.HttpMethod.ToUpper();

            HttpMethod hm;
            switch (method)
            {
                case "GET":
                    hm = HttpMethod.GET;
                    break;
                case "POST":
                    hm = HttpMethod.POST;
                    break;
                case "PUT":
                    hm = HttpMethod.PUT;
                    break;
                case "DELETE":
                    hm = HttpMethod.DELETE;
                    break;
                default:
#if DEBUG
                    Console.WriteLine($"Invalid HTTP method: {method} from {context.Request.LocalEndPoint}");
#endif
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    context.Response.Close();
                    return;
            }

            bool generalFallback;
            var publicFile = !string.IsNullOrEmpty(PublicDir) && !route.TrimStart('/').Contains("/") ? Path.Combine(PublicDir, route.TrimStart('/')) : "";
            var act = _rtman.SearchInTree(route, hm, out generalFallback);
            if (generalFallback && File.Exists(publicFile))
                new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
            else if (act != null)
                act.Action(new RRequest(context.Request, GetParams(act, route)),
                    new RResponse(context.Response, _rPluginCollection));
            else
            {
                context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                context.Response.Close();
            }
        }

        private static RequestParams GetParams(RHttpAction act, string route)
        {
            var rTree = route.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var rLen = rTree.Length;
            var dict = act.Params
                .Where(kvp => kvp.Key < rLen)
                .ToDictionary(kvp => kvp.Value, kvp => rTree[kvp.Key]);
            return new RequestParams(dict);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}