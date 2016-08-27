using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RHttpServer.Security
{
    /// <summary>
    /// The default security handler
    /// </summary>
    public sealed class SimpleHttpSecurityHandler : IHttpSecurityHandler
    {
        private readonly IDictionary<IPAddress, Visitor> _visitors = new ConcurrentDictionary<IPAddress, Visitor>();
        private readonly Thread _maintainerThread;
        private volatile bool _maintainerRunning;
        private bool _started;

        public SimpleHttpSecurityHandler()
        {
            _maintainerThread = new Thread(MaintainVisitorList);
        }

        public bool HandleRequest(HttpListenerRequest req)
        {
            var url = req.RemoteEndPoint?.Address;
            if (url != null && Settings != null)
            {
                Visitor vis;
                if (_visitors.TryGetValue(url, out vis))
                {
                    if (vis.RequestsInSession > Settings.MaxRequestsPerSession) return false;
                    vis.JustRequested();
                }
                else
                {
                    _visitors.Add(url, new Visitor());
                }
            }
            return true;
        }

        public void Start()
        {
            if (_started) return;
            _maintainerRunning = true;
            _maintainerThread.Start();
            _started = true;
        }

        private async void MaintainVisitorList()
        {
            while (_maintainerRunning)
            {
                await Task.Delay(Settings.SessionLengthSeconds*1000);
                var now = DateTime.Now;
                var olds = _visitors.Where(t => now.Subtract((DateTime) t.Value.Visited).TotalSeconds > Settings.SessionLengthSeconds).Select(t => t.Key);
                foreach (var ipAddress in olds)
                {
                    _visitors.Remove(ipAddress);
                }
            }
        }

        public void Stop()
        {
            if (!_started) return;
            _maintainerRunning = false;
            _maintainerThread.Join(250);
            _started = false;
        }

        public IHttpSecuritySettings Settings { get; set; }
    }
}