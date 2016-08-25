// MIT License - Copyright (c) 2016 Can Güney Aksakalli

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WebServerHoster
{
    public class SimpleHttpServer
    {
        public string PublicDir { get; }
        public int Port { get; }

        private readonly string[] _indexFiles = {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        private readonly IDictionary<string, Action<SimpleRequest, SimpleResponse>> _getActions
            = new Dictionary<string, Action<SimpleRequest, SimpleResponse>>();

        private readonly IDictionary<string, Action<SimpleRequest, SimpleResponse>> _postActions
            = new Dictionary<string, Action<SimpleRequest, SimpleResponse>>();

        private readonly IDictionary<string, Action<SimpleRequest, SimpleResponse>> _putActions
            = new Dictionary<string, Action<SimpleRequest, SimpleResponse>>();

        private readonly IDictionary<string, Action<SimpleRequest, SimpleResponse>> _deleteActions 
            = new Dictionary<string, Action<SimpleRequest, SimpleResponse>>();
        
        private Thread _serverThread;
        private HttpListener _listener;


        public void Get(string route, Action<SimpleRequest, SimpleResponse> action)
        {
            if (!route.StartsWith("/")) route = "/" + route;
            if (route.Contains(":"))
            {
                var matches = Regex.Matches(route, ":[\\w\\d-.,]+", RegexOptions.Compiled);
                if (matches.Count == 0) throw new SimpleHttpServerException("The route may not contain a colon (':') without it being immediately followed by a identifier");

                List<string> list = (from object match in matches select match.ToString()).ToList();
                foreach (var m in list)
                {
                    route = route.Replace(m, "");
                }
            }
            _getActions.Add(route, action);
        }

        public void Post(string route, Action<SimpleRequest, SimpleResponse> action)
        {
            if (!route.StartsWith("/")) route = "/" + route;
            _getActions.Add(route, action);
        }

        public void Put(string route, Action<SimpleRequest, SimpleResponse> action)
        {
            if (!route.StartsWith("/")) route = "/" + route;
            _putActions.Add(route, action);
        }

        public void Delete(string route, Action<SimpleRequest, SimpleResponse> action)
        {
            if (!route.StartsWith("/")) route = "/" + route;
            _deleteActions.Add(route, action);
        }
        

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public SimpleHttpServer(string path, int port)
        {
            PublicDir = path;
            Port = port;
            Initialize();
        }

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public SimpleHttpServer(string path)
        {
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            Initialize();
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:" + Port + "/");
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

            IDictionary<string, Action<SimpleRequest, SimpleResponse>> dict;
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
            
            Action<SimpleRequest, SimpleResponse> act;
            if (dict.TryGetValue(route, out act))
            {
                Task.Run(() => act(new SimpleRequest(context.Request), new SimpleResponse(context.Response)));
            }
            else
            {
                var publicFile = Path.Combine(PublicDir, route);
                if (File.Exists(publicFile))
                {
                    Task.Run(() => new SimpleResponse(context.Response).SendFile(publicFile));
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.OutputStream.Close();
                }
            }
        }
        

        private void Initialize()
        {
            _serverThread = new Thread(Listen);
            _serverThread.Start();
        }
    }
}