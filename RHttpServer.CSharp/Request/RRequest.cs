using System.IO;
using System.Net;

namespace RHttpServer.Request
{
    /// <summary>
    ///     Class representing a request from a client
    /// </summary>
    public class RRequest
    {
        internal RRequest(HttpListenerRequest req, RequestParams par)
        {
            UnderlyingRequest = req;
            Params = par;
            Cookies = new RCookies(req.Cookies);
            Headers = new RHeaders(req.Headers);
        }

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
        ///     Returns the MIME type for the body
        /// </summary>
        /// <returns>The MIME type as a string</returns>
        public string GetBodyMimeType() => UnderlyingRequest.ContentType;

        /// <summary>
        ///     Returns the body of the request
        ///     and null if the request does not contain a body
        /// </summary>
        /// <returns>The request body as a string</returns>
        public string GetBody()
        {
            if (!UnderlyingRequest.HasEntityBody) return null;
            using (var body = UnderlyingRequest.InputStream)
            {
                using (var reader = new StreamReader(body, UnderlyingRequest.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}