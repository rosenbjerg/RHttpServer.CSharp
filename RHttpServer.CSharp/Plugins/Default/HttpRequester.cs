using System;

namespace RHttpServer.Plugins.Default
{
    internal class HttpRequester
    {
        internal HttpRequester()
        {
            RequestsInSession = 1;
            SessionStarted = DateTime.Now;
        }

        private readonly object _lock = new object();

        public DateTime SessionStarted { get; }

        public int RequestsInSession { get; private set; }

        public DateTime LatestVisit { get; private set; }

        public int JustRequested()
        {
            lock (_lock)
            {
                RequestsInSession++;
            }
            LatestVisit = DateTime.Now;
            return RequestsInSession;
        }
    }
}