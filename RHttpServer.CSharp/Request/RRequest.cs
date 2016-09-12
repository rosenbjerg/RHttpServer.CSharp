using System.Net;
using System.Threading.Tasks;
using RHttpServer.Plugins;

namespace RHttpServer.Request
{
    /// <summary>
    ///     Class representing a request from a client
    /// </summary>
    public class RRequest
    {
        internal RRequest(HttpListenerRequest req, RequestParams par, RPluginCollection pluginCollection)
        {
            UnderlyingRequest = req;
            Params = par;
            Cookies = new RCookies(req.Cookies);
            Headers = new RHeaders(req.Headers);
            Queries = new RQueries(req.QueryString);
            var da = Queries["id"];
            _bodyParser = pluginCollection.Use<IBodyParser>();
        }

        public RQueries Queries { get; set; }

        private readonly IBodyParser _bodyParser;

        /// <summary>
        ///     The headers contained in the request
        /// </summary>
        public RHeaders Headers { get; set; }

        /// <summary>
        ///     The cookies contained in the request
        /// </summary>
        public RCookies Cookies { get; }

        /// <summary>
        ///     The url parameters of the request
        /// </summary>
        public RequestParams Params { get; }

        /// <summary>
        ///     The underlying HttpListenerRequest
        ///     This implementation of RRequest is leaky, to avoid limiting you
        /// </summary>
        public HttpListenerRequest UnderlyingRequest { get; }

        /// <summary>
        ///     Returns the body of the request
        ///     and null if the request does not contain a body
        /// </summary>
        /// <returns>The request body as a string</returns>
        public Task<T> GetBody<T>()
        {
            return _bodyParser.ParseBody<T>(UnderlyingRequest);
        }
    }
}