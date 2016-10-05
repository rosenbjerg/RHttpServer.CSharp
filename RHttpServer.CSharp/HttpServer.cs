using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using RHttpServer.Logging;
using RHttpServer.Plugins;
using RHttpServer.Plugins.Default;
using RHttpServer.Request;
using RHttpServer.Response;

namespace RHttpServer
{
    /// <summary>
    ///     Represents a HTTP server.
    ///     It should be set up before start
    /// </summary>
    public class HttpServer : IDisposable
    {
        internal static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        internal static bool ThrowExceptions;

        /// <summary>
        ///     Constructs a server instance with given port and using the given path as public folder.
        ///     Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="port">The port that the server should listen on</param>
        /// <param name="requestHandlerThreads">The amount of threads to handle the incoming requests</param>
        /// <param name="throwExceptions">Whether exceptions should be suppressed and logged, or thrown</param>
        public HttpServer(int port, int requestHandlerThreads, string path = "", bool throwExceptions = false)
        {
            if (requestHandlerThreads < 1)
            {
                requestHandlerThreads = 1;
                Logger.Log("Thread setting", "Minimum 1 request-handler threads");
            }
            ThrowExceptions = throwExceptions;
            PublicDir = path;
            Port = port;
            _workers = new Thread[requestHandlerThreads];
            _queue = new ConcurrentQueue<HttpListenerContext>();
            _stop = new ManualResetEventSlim(false);
            _ready = new ManualResetEventSlim(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests) {Name = "ListenerThread"};
        }

        /// <summary>
        ///     Constructs a server instance with an automatically found port and using the given path as public folder.
        ///     Set path to null or empty string if none wanted
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="requestHandlerThreads">The amount of threads to handle the incoming requests</param>
        /// <param name="throwExceptions">Whether exceptions should be suppressed and logged, or thrown</param>
        public HttpServer(int requestHandlerThreads, string path = "", bool throwExceptions = false)
        {
            if (requestHandlerThreads < 1)
            {
                requestHandlerThreads = 1;
                Logger.Log("Thread setting", "Minimum 1 request-handler threads");
            }
            ThrowExceptions = throwExceptions;
            PublicDir = path;
            //get an empty port
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            Port = ((IPEndPoint) l.LocalEndpoint).Port;
            l.Stop();
            _workers = new Thread[requestHandlerThreads];
            _queue = new ConcurrentQueue<HttpListenerContext>();
            _stop = new ManualResetEventSlim(false);
            _ready = new ManualResetEventSlim(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests) {Name = "ListenerThread"};
        }

        private readonly string[] _indexFiles =
        {
            "index.html",
            "index.htm",
            "index.php",
            "default.html",
            "default.htm",
            "default.php"
        };


        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly ConcurrentQueue<HttpListenerContext> _queue;
        private readonly RPluginCollection _rPluginCollection = new RPluginCollection();
        private readonly RouteTreeManager _rtman = new RouteTreeManager();
        private readonly ManualResetEventSlim _stop, _ready;
        private readonly Thread[] _workers;
        private IFileCacheManager _cacheMan;
        private bool _defPluginsReady;
        private bool _securityOn;

        /// <summary>
        ///     The publicly available folder
        /// </summary>
        public string PublicDir { get; }

        /// <summary>
        ///     Whether public files should be cached if size and extension is set to be cached
        /// </summary>
        public bool CachePublicFiles { get; set; }

        /// <summary>
        ///     The port that the server is listening on
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Whether security is turned on
        /// </summary>
        public bool SecurityOn
        {
            get { return _securityOn; }
            set
            {
                if (_securityOn == value) return;
                _securityOn = value;
                if (_securityOn)
                    _rPluginCollection.Use<IHttpSecurityHandler>().Start();
                else
                    _rPluginCollection.Use<IHttpSecurityHandler>().Stop();
            }
        }


        /// <summary>
        ///     Whether the server should respond to http requests
        ///     <para />
        ///     Defaults to true
        /// </summary>
        public bool HttpEnabled { get; set; } = true;

        /// <summary>
        ///     Whether the server should respond to https requests
        ///     <para />
        ///     You must have a (ssl) certificate installed to the specified port for it to respond.
        /// </summary>
        public bool HttpsEnabled { get; set; }

        /// <summary>
        ///     The port that https requests are handled through, if enabled
        /// </summary>
        public int HttpsPort { get; set; } = 5443;

        /// <summary>
        ///     Register a plugin to be used in the server.
        ///     <para />
        ///     You can replace the default plugins by registering your plugin using the same interface as key before starting the
        ///     server
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

        /// <summary>
        ///     Add action to handle GET requests to a given route
        ///     <para />
        ///     Should always be idempotent.
        ///     (Receiving the same GET request one or multiple times should yield same result)
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
        ///     Add action to handle PUT requests to a given route.
        ///     <para />
        ///     Should always be idempotent.
        ///     (Receiving the same PUT request one or multiple times should yield same result)
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Put(string route, Action<RRequest, RResponse> action)
            => _rtman.AddRoute(new RHttpAction(route, action), HttpMethod.PUT);

        /// <summary>
        ///     Add action to handle DELETE requests to a given route.
        ///     <para />
        ///     Should always be idempotent.
        ///     (Receiving the same DELETE request one or multiple times should yield same result)
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Delete(string route, Action<RRequest, RResponse> action)
            => _rtman.AddRoute(new RHttpAction(route, action), HttpMethod.DELETE);

        /// <summary>
        ///     Add action to handle HEAD requests to a given route
        ///     You should only send the headers of the route as response
        ///     <para />
        ///     Should always be idempotent.
        ///     (Receiving the same HEAD request one or multiple times should yield same result)
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Head(string route, Action<RRequest, RResponse> action)
            => _rtman.AddRoute(new RHttpAction(route, action), HttpMethod.HEAD);

        /// <summary>
        ///     Add action to handle OPTIONS requests to a given route
        ///     You should respond only using headers.
        ///     <para />
        ///     Should always be idempotent.
        ///     (Receiving the same HEAD request one or multiple times should yield same result)
        ///     <para />
        ///     And should contain one header with the id "Allow", and the content should contain the HTTP methods the route
        ///     allows.
        ///     <para />
        ///     (f.x. "Allow": "GET, POST, OPTIONS")
        /// </summary>
        /// <param name="route">The route to respond to</param>
        /// <param name="action">The action that wil respond to the request</param>
        public void Options(string route, Action<RRequest, RResponse> action)
            => _rtman.AddRoute(new RHttpAction(route, action), HttpMethod.OPTIONS);

        /// <summary>
        ///     Starts the server, and all request handling threads
        ///     <para />
        /// </summary>
        /// <param name="localOnly">Whether to only listn locally</param>
        public void Start(bool localOnly = false)
        {
            Start(localOnly ? "localhost" : "+");
        }

        /// <summary>
        ///     Starts the server, and all request handling threads <para />
        ///     Only answers to requests with specified prefixes <para />
        ///     Specify in following format: <para />
        ///     "+" , "*" , "localhost" , "example.com", "123.12.34.56"<para />
        ///     Protocol and port will be added automatically
        /// </summary>
        /// <param name="listeningPrefixes">The prefixes the server will listen for requests with</param>
        public void Start(params string[] listeningPrefixes) 
        {
            try
            {
                InitializeDefaultPlugins();
                foreach (var listeningPrefix in listeningPrefixes)
                {
                    if (HttpEnabled) _listener.Prefixes.Add($"http://{listeningPrefix}:{Port}/");
                    if (HttpsEnabled) _listener.Prefixes.Add($"https://{listeningPrefix}:{HttpsPort}/");
                }
                if (_listener.Prefixes.Count == 0)
                {
                    Console.WriteLine("You must listen for either http or https (or both) requests for the server to do anything");
                    return;
                }
                _listener.Start();
                _listenerThread.Start();

                for (var i = 0; i < _workers.Length; i++)
                {
                    _workers[i] = new Thread(Worker) { Name = $"RequestHandler #{i}" };
                    _workers[i].Start();
                }
                Console.WriteLine("RHttpServer v. {0} started", Version);
                if (_listener.Prefixes.First() == "localhost") Logger.Log("Server visibility", "Listening on localhost only");
                RenderParams.Renderer = _rPluginCollection.Use<IPageRenderer>();
            }
            catch (SocketException)
            {
                Console.WriteLine("Unable to bind to port, the port may already be in use.");
                Environment.Exit(0);
            }
            catch (HttpListenerException)
            {
                Console.WriteLine("Could not obtain permission to listen for '{0}' on selected port\n" +
                                  "Please aquire the permission or start the server as local-only", string.Join(", ", listeningPrefixes));
                Environment.Exit(0);
            }
        }

        /// <summary>
        ///     Initializes any default plugin if no other plugin is registered to same interface
        ///     <para />
        ///     Also used for changing the default security settings
        ///     <para />
        ///     Should be called after you have registered all your non-default plugins
        /// </summary>
        public void InitializeDefaultPlugins(bool renderCaching = true, bool securityOn = false,
            SimpleHttpSecuritySettings securitySettings = null)
        {
            if (_defPluginsReady) return;

            if (!_rPluginCollection.IsRegistered<IJsonConverter>())
                RegisterPlugin<IJsonConverter, ServiceStackJsonConverter>(new ServiceStackJsonConverter());

            if (!_rPluginCollection.IsRegistered<IXmlConverter>())
                RegisterPlugin<IXmlConverter, ServiceStackXmlConverter>(new ServiceStackXmlConverter());

            if (!_rPluginCollection.IsRegistered<IHttpSecurityHandler>())
                RegisterPlugin<IHttpSecurityHandler, SimpleServerProtection>(new SimpleServerProtection());

            if (!_rPluginCollection.IsRegistered<IBodyParser>())
                RegisterPlugin<IBodyParser, SimpleBodyParser>(new SimpleBodyParser());

            if (!_rPluginCollection.IsRegistered<IFileCacheManager>())
                RegisterPlugin<IFileCacheManager, SimpleFileCacheManager>(new SimpleFileCacheManager());

            if (!_rPluginCollection.IsRegistered<IPageRenderer>())
                RegisterPlugin<IPageRenderer, EcsPageRenderer>(new EcsPageRenderer());

            _defPluginsReady = true;

            if (securitySettings == null) securitySettings = new SimpleHttpSecuritySettings();
            _rPluginCollection.Use<IHttpSecurityHandler>().Settings = securitySettings;
            _rPluginCollection.Use<IPageRenderer>().CachePages = renderCaching;
            _cacheMan = _rPluginCollection.Use<IFileCacheManager>();
            SecurityOn = securityOn;
        }

        /// <summary>
        ///     Stops the server thread and all request handling threads.
        /// </summary>
        public void Stop()
        {
            _stop.Set();
            _listenerThread.Join();
            foreach (var worker in _workers)
                worker.Join(100);
            _listener.Stop();
            _rPluginCollection.Use<IHttpSecurityHandler>().Stop();
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
            catch (Exception ex)
            {
                Logger.Log(ex);
                if (ThrowExceptions) throw;
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                _queue.Enqueue(_listener.EndGetContext(ar));
                _ready.Set();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                if (ThrowExceptions) throw;
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
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    if (ThrowExceptions) throw;
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
                case "OPTIONS":
                    hm = HttpMethod.OPTIONS;
                    break;
                case "HEAD":
                    hm = HttpMethod.HEAD;
                    break;
                default:
                    Logger.Log("Invalid HTTP method", $"{method} from {context.Request.LocalEndPoint}");
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    context.Response.Close();
                    return;
            }

            bool generalFallback;

            var act = _rtman.SearchInTree(route, hm, out generalFallback);
            if ((generalFallback || (act == null)) && !string.IsNullOrWhiteSpace(PublicDir))
            {
                var p = route.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries).ToList();
                p.Insert(0, PublicDir);
                byte[] temp = null;
                var publicFile = Path.Combine(p.ToArray());
                if (CachePublicFiles && _cacheMan.TryGetFile(publicFile, out temp))
                {
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, "text/html");
                    return;
                }
                if (File.Exists(publicFile))
                {
                    temp = File.ReadAllBytes(publicFile);
                    var type = "";
                    if (!RResponse.MimeTypes.TryGetValue(Path.GetExtension(publicFile).ToLowerInvariant(), out type))
                        type = "application/octet-stream";
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, type);
                    if (CachePublicFiles && _cacheMan.CanAdd(temp.Length, publicFile))
                        _cacheMan.TryAdd(publicFile, temp);
                    return;
                }
                var pfiles = _indexFiles.Select(x => Path.Combine(publicFile, x));
                if (CachePublicFiles)
                {
                    foreach (var iFile in pfiles) if (_cacheMan.TryGetFile(iFile, out temp)) break;
                    if (temp != null)
                    {
                        new RResponse(context.Response, _rPluginCollection).SendBytes(temp, "text/html");
                        return;
                    }
                }
                publicFile = pfiles.FirstOrDefault(File.Exists);

                if (!string.IsNullOrEmpty(publicFile))
                {
                    temp = File.ReadAllBytes(publicFile);
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, "text/html");
                    if (CachePublicFiles && _cacheMan.CanAdd(temp.Length, publicFile))
                        _cacheMan.TryAdd(publicFile, temp);
                    return;
                }
            }
            if (act != null)
            {
                RRequest req;
                RResponse res;
                CreateReqRes(context, GetParams(act, route), _rPluginCollection, out req, out res);
                act.Action(req, res);
            }
            else
            {
                context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                context.Response.Close();
            }
        }

        private static void CreateReqRes(HttpListenerContext context, RequestParams reqPar, RPluginCollection plugins,
            out RRequest req, out RResponse res)
        {
            req = new RRequest(context.Request, reqPar, plugins);
            res = new RResponse(context.Response, plugins);
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