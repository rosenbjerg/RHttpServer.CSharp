using System.Net;

namespace WebServerHoster
{
    public class SimpleRequest
    {
        public SimpleRequest(HttpListenerRequest req)
        {
            UnderlyingRequest = req;
        }

        public string GetBody()
        {
            if (!UnderlyingRequest.HasEntityBody) return null;
            using (System.IO.Stream body = UnderlyingRequest.InputStream) // here we have data
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, UnderlyingRequest.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        

        public HttpListenerRequest UnderlyingRequest { get; }
    }
}