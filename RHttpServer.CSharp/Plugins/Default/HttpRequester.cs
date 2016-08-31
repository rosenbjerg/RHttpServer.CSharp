using System;

namespace RHttpServer.Core.Plugins.Default
{
    internal class HttpRequester
    {
        internal HttpRequester()
        {
            RequestsInSession = 1;
            SessionStarted = DateTime.Now;
        }

        public DateTime SessionStarted { get; }

        public int RequestsInSession { get; private set; }
        
        private readonly object _lock = new object();
        public int JustRequested()
        {
            lock (_lock)
            {
                RequestsInSession++;
            }
            LatestVisit = DateTime.Now;
            return RequestsInSession;
        }

        public DateTime LatestVisit { get; private set; }
    }
}