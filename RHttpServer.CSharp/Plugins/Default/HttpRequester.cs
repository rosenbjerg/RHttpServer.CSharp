using System;

namespace RHttpServer.Plugins.Default
{
    internal class HttpRequester
    {
        internal HttpRequester()
        {
            RequestsInSession = 1;
            Visited = DateTime.Now;
        }

        public DateTime Visited { get; }

        public int RequestsInSession { get; private set; }
        
        private readonly object _lock = new object();
        public int JustRequested()
        {
            lock (_lock)
            {
                RequestsInSession++;
            }
            return RequestsInSession;
        }
    }
}