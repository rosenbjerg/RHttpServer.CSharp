using System.Net;

namespace WebServerHoster
{
    public class SimpleRequest
    {
        public SimpleRequest(HttpListenerRequest req, RequestParams par)
        {
            UnderlyingRequest = req;
            Params = par;
        }
        
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
        
        public RequestParams Params { get; }
        public HttpListenerRequest UnderlyingRequest { get; }
    }
}