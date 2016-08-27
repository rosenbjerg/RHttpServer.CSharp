using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RHttpServer.Plugins.Default
{
    /// <summary>
    /// The default security handler
    /// </summary>
    public sealed class SimpleHttpSecurityHandler : SimplePlugin, IHttpSecurityHandler
    {
        private readonly ConcurrentDictionary<string, HttpRequester> _visitors = new ConcurrentDictionary<string, HttpRequester>();
        private readonly ConcurrentDictionary<string, bool> _blacklist = new ConcurrentDictionary<string, bool>();
        private readonly Thread _visitorMaintainerThread;
        private readonly Thread _blacklistMaintainerThread;
        private volatile bool _maintainerRunning;
        private bool _started;

        internal SimpleHttpSecurityHandler()
        {
            _visitorMaintainerThread = new Thread(MaintainVisitorList);
            _blacklistMaintainerThread = new Thread(MaintainBlacklist);
        }

        public bool HandleRequest(HttpListenerRequest req)
        {
            var url = req.RemoteEndPoint?.Address.MapToIPv4().ToString();
            if (url != null && Settings != null)
            {
                bool found;
                HttpRequester vis;
                if (_blacklist.TryGetValue(url, out found))
                {
                    return false;
                }

                if (_visitors.TryGetValue(url, out vis))
                {
                    if (vis.JustRequested() <= Settings.MaxRequestsPerSession) return true;
                    _blacklist.TryAdd(url, true);
                    _visitors.TryRemove(url, out vis);
                    Console.WriteLine($"{url} has been blacklisted for {Settings.BanTimeMinutes} minutes");
                    return true;
                }
                _visitors.TryAdd(url, new HttpRequester());
            }
            return true;
        }

        public void Start()
        {
            if (_started) return;
            _maintainerRunning = true;
            _visitorMaintainerThread.Start();
            _blacklistMaintainerThread.Start();
            _started = true;
        }

        private async void MaintainVisitorList()
        {
            while (_maintainerRunning)
            {
                await Task.Delay(TimeSpan.FromSeconds(Settings.SessionLengthSeconds/2));
                var now = DateTime.Now;
                var olds = _visitors.Where(t => now.Subtract(t.Value.Visited).TotalSeconds > Settings.SessionLengthSeconds).Select(t => t.Key);
                foreach (var ipAddress in olds)
                {
                    HttpRequester vis = null;
                    _visitors.TryRemove(ipAddress, out vis);
                }
            }
        }

        private async void MaintainBlacklist()
        {
            while (_maintainerRunning)
            {
                await Task.Delay(TimeSpan.FromMinutes(Settings.BanTimeMinutes/2.0));
                var now = DateTime.Now;
                var olds = _visitors.Where(t => now.Subtract(t.Value.Visited).TotalSeconds > Settings.SessionLengthSeconds).Select(t => t.Key);
                foreach (var ipAddress in olds)
                {
                    HttpRequester vis = null;
                    _visitors.TryRemove(ipAddress, out vis);
                }
            }
        }

        public void Stop()
        {
            if (!_started) return;
            _maintainerRunning = false;
            _visitorMaintainerThread.Join(100);
            _blacklistMaintainerThread.Join(100);
            _started = false;
        }

        public IHttpSecuritySettings Settings { get; set; }
    }
}