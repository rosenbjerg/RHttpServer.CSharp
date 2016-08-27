using System.Net;

namespace RHttpServer
{
    /// <summary>
    /// Class representing a request from a client
    /// </summary>
    public class SimpleRequest
    {
        internal SimpleRequest(HttpListenerRequest req, RequestParams par)
        {
            UnderlyingRequest = req;
            Params = par;
        }

        /// <summary>
        /// Returns the MIME type for the body
        /// </summary>
        /// <returns>The MIME type as a string</returns>
        public string GetBodyMimeType() => UnderlyingRequest.ContentType;

        /// <summary>
        /// Returns the body of the request 
        /// and null if the request does not contain a body
        /// </summary>
        /// <returns>The request body as a string</returns>
        public string GetBody()
        {
            if (!UnderlyingRequest.HasEntityBody) return null;
            using (System.IO.Stream body = UnderlyingRequest.InputStream)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, UnderlyingRequest.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        
        /// <summary>
        /// The url parameters of the request
        /// </summary>
        public RequestParams Params { get; }

        /// <summary>
        /// The underlying HttpListenerRequest
        /// This implementation of SimpleRequest is leaky, to avoid limiting you
        /// </summary>
        public HttpListenerRequest UnderlyingRequest { get; }
    }
}