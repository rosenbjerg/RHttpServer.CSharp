using System;

namespace RHttpServer.Security
{
    public class Visitor
    {
        public Visitor()
        {
            RequestsInSession = 1;
            Visited = DateTime.Now;
        }

        public DateTime Visited { get; }

        public int RequestsInSession { get; private set; }

        public void JustRequested()
        {
            RequestsInSession++;
        }
    }
}