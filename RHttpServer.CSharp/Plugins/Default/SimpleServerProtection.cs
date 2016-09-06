using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RHttpServer.Plugins.Default
{
    /// <summary>
    ///     The default security handler
    /// </summary>
    internal sealed class SimpleServerProtection : RPlugin, IHttpSecurityHandler
    {
        internal SimpleServerProtection()
        {
            _visitorMaintainerThread = new Thread(MaintainVisitorList);
            _blacklistMaintainerThread = new Thread(MaintainBlacklist);
        }

        private readonly ConcurrentDictionary<string, byte> _blacklist = new ConcurrentDictionary<string, byte>();
        private readonly Thread _blacklistMaintainerThread;
        private readonly Thread _visitorMaintainerThread;

        private readonly ConcurrentDictionary<string, HttpRequester> _visitors =
            new ConcurrentDictionary<string, HttpRequester>();

        private volatile bool _maintainerRunning;
        private bool _started;

        private async void MaintainVisitorList()
        {
            while (_maintainerRunning)
            {
                await Task.Delay(TimeSpan.FromSeconds(Settings.SessionLengthSeconds/2.0));
                var now = DateTime.Now;
                var olds =
                    _visitors.Where(
                        t => now.Subtract(t.Value.SessionStarted).TotalSeconds > Settings.SessionLengthSeconds)
                        .Select(t => t.Key);
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
                var olds =
                    _visitors.Where(
                        t => now.Subtract(t.Value.SessionStarted).TotalSeconds > Settings.SessionLengthSeconds)
                        .Select(t => t.Key);
                foreach (var ipAddress in olds)
                {
                    HttpRequester vis = null;
                    _visitors.TryRemove(ipAddress, out vis);
                }
            }
        }

        public bool HandleRequest(HttpListenerRequest req)
        {
            var url = req.RemoteEndPoint?.Address.MapToIPv4().ToString();
            if (url != null && Settings != null)
            {
                HttpRequester vis;
                if (_blacklist.ContainsKey(url)) return false;

                if (_visitors.TryGetValue(url, out vis))
                {
                    if (vis.JustRequested() <= Settings.MaxRequestsPerSession) return true;
                    _blacklist.TryAdd(url, 1);
                    _visitors.TryRemove(url, out vis);

                    Logging.Logger.Log("Security", $"{url} has been blacklisted for {Settings.BanTimeMinutes} minutes");
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