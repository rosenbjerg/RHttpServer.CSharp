using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        /// Whether a header containing server info should be included
        /// </summary>
        public static bool IncludeServerHeader { get; set; } = true;

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
            _publicFiles = Directory.Exists(path);
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
            _publicFiles = Directory.Exists(path);
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
        private IHttpSecurityHandler _secMan;
        private bool _defPluginsReady;
        private bool _securityOn;
        private bool _publicFiles;

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
                    _rPluginCollection.Use<IHttpSecurityHandler>()?.Start();
                else
                    _rPluginCollection.Use<IHttpSecurityHandler>()?.Stop();
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
        ///     You must have a (ssl) certificate setup to the specified port for it to respond.
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
        ///     Should contain one header with the id "Allow", and the content should contain the HTTP methods the route
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
                _listener.IgnoreWriteExceptions = true;
                _listener.Start();
                _listenerThread.Start();

                for (var i = 0; i < _workers.Length; i++)
                {
                    _workers[i] = new Thread(Worker) { Name = $"ReqHandler #{i}" };
                    _workers[i].Start();
                }
                Console.WriteLine("RHttpServer v. {0} started", Version);
                if (_listener.Prefixes.First() == "localhost") Logger.Log("Server visibility", "Listening on localhost only");
                RenderParams.Renderer = _rPluginCollection.Use<IPageRenderer>();
                _publicFiles = !string.IsNullOrWhiteSpace(PublicDir);
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
        private Func<string, RHttpAction, HttpListenerContext, bool, bool> _reqHandler;
        private ResponseHandler _resHandler;

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
            _secMan = _rPluginCollection.Use<IHttpSecurityHandler>();
            _secMan.Settings = securitySettings;
            _rPluginCollection.Use<IPageRenderer>().CachePages = renderCaching;
            _cacheMan = _rPluginCollection.Use<IFileCacheManager>();
            if (CachePublicFiles && _publicFiles)
                _resHandler = new CachePublicFileRequestHander(PublicDir, _cacheMan, _rPluginCollection);
            else if (_publicFiles)
                _resHandler = new PublicFileRequestHander(PublicDir, _rPluginCollection);
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
                if (ThrowExceptions) throw;
                Logger.Log(ex);
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
                if (ThrowExceptions) throw;
                Logger.Log(ex);
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
                if (!SecurityOn || _secMan.HandleRequest(context.Request))
                    Process(context);
            }
        }
        
        private void Process(HttpListenerContext context)
        {
            var route = context.Request.Url.AbsolutePath.Trim('/');
            HttpMethod hm = GetMethod(context.Request.HttpMethod);
            if (hm == HttpMethod.UNKNOWN)
            {
                Logger.Log("Invalid HTTP method", $"{context.Request.HttpMethod} from {context.Request.RemoteEndPoint}");
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            bool generalFallback;
            var act = _rtman.SearchInTree(route, hm, out generalFallback);
            if ((act == null || generalFallback) && _publicFiles && _resHandler.Handle(route, context))
                return;

            if (act != null)
            {
                RRequest req;
                RResponse res;
                GetReqRes(context, GetParams(act, route), _rPluginCollection, out req, out res);
                act.Action(req, res);
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }
        
        private static HttpMethod GetMethod(string input)
        {
            switch (input)
            {
                case "GET":
                    return HttpMethod.GET;
                case "POST":
                    return HttpMethod.POST;
                case "PUT":
                    return HttpMethod.PUT;
                case "DELETE":
                    return HttpMethod.DELETE;
                case "OPTIONS":
                    return HttpMethod.OPTIONS;
                case "HEAD":
                    return HttpMethod.HEAD;
                default:
                    return HttpMethod.UNKNOWN;
            }
        }

        private static void GetReqRes(HttpListenerContext context, RequestParams reqPar, RPluginCollection plugins,
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

        public T GetPlugin<T>()
        {
            return _rPluginCollection.Use<T>();
        }
    }
    
    abstract class ResponseHandler
    {
        public abstract bool Handle(string route, HttpListenerContext context);

        protected static void GetRange(string range, out int rangeStart, out int rangeEnd)
        {
            var split = range.Split('-');
            if (string.IsNullOrEmpty(split[0]))
                rangeStart = -1;
            else
                int.TryParse(split[0], out rangeStart);
            if (string.IsNullOrEmpty(split[1]))
                rangeEnd = -1;
            else
                int.TryParse(split[1], out rangeEnd);
        }

        protected static string GetType(string input)
        {
            string ret;
            if (!RResponse.MimeTypes.TryGetValue(Path.GetExtension(input), out ret))
                ret = "application/octet-stream";
            return ret;
        }

        protected readonly string[] _indexFiles =
        {
            "index.html",
            "index.htm",
            "index.php",
            "default.html",
            "default.htm",
            "default.php"
        };
    }

    sealed class PublicFileRequestHander : ResponseHandler
    {
        private readonly string _pdir;
        private readonly IFileCacheManager _cacheMan;
        private readonly RPluginCollection _rPluginCollection;

        public PublicFileRequestHander(string publicDir, RPluginCollection rplugins)
        {
            _pdir = publicDir;
            _rPluginCollection = rplugins;
        }

        public override bool Handle(string route, HttpListenerContext context)
        {
            var range = context.Request.Headers["Range"];
            bool rangeSet = false;
            int rangeStart = 0, rangeEnd = 0;
            if (!string.IsNullOrEmpty(range))
            {
                range = range.Replace("bytes=", "");
                GetRange(range, out rangeStart, out rangeEnd);
                rangeSet = true;
            }

            var publicFile = Path.Combine(_pdir, route);
            
            if (File.Exists(publicFile))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile, rangeStart, rangeEnd);
                else
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                return true;
            }

            var pfiles = _indexFiles.Select(x => Path.Combine(publicFile, x));
            if (!string.IsNullOrEmpty(publicFile = pfiles.FirstOrDefault(File.Exists)))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile, rangeStart, rangeEnd);
                else
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                return true;
            }
            return false;
        }
    }

    sealed class CachePublicFileRequestHander : ResponseHandler
    {
        private readonly string _pdir;
        private readonly IFileCacheManager _cacheMan;
        private readonly RPluginCollection _rPluginCollection;

        public CachePublicFileRequestHander(string publicDir, IFileCacheManager cache, RPluginCollection rplugins)
        {
            _pdir = publicDir;
            _cacheMan = cache;
            _rPluginCollection = rplugins;
        }

        public override bool Handle(string route, HttpListenerContext context)
        {
            var range = context.Request.Headers["Range"];
            bool rangeSet = false;
            int rangeStart = 0, rangeEnd = 0;
            if (!string.IsNullOrEmpty(range))
            {
                range = range.Replace("bytes=", "");
                GetRange(range, out rangeStart, out rangeEnd);
                rangeSet = true;
            }


            var publicFile = Path.Combine(_pdir, route);

            byte[] temp = null;
            if (_cacheMan.TryGetFile(publicFile, out temp))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, rangeStart, rangeEnd, GetType(publicFile), publicFile);
                else
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, GetType(publicFile), publicFile);
                return true;
            }

            if (File.Exists(publicFile))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile, rangeStart, rangeEnd);
                else
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                _cacheMan.TryAddFile(publicFile);
                return true;
            }

            var pfiles = _indexFiles.Select(x => Path.Combine(publicFile, x)).ToList();
            if (!string.IsNullOrEmpty(publicFile = pfiles.FirstOrDefault(iFile => _cacheMan.TryGetFile(iFile, out temp))))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, rangeStart, rangeEnd, GetType(publicFile), publicFile);
                else
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, GetType(publicFile), publicFile);
                return true;
            }
            if (!string.IsNullOrEmpty(publicFile = pfiles.FirstOrDefault(File.Exists)))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile, rangeStart, rangeEnd);
                else
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                _cacheMan.TryAddFile(publicFile);
                return true;
            }
            return false;
        }
    }
    
}