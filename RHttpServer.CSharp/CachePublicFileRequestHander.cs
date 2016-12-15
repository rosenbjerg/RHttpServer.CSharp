using System.IO;
using System.Linq;
using System.Net;
using RHttpServer.Plugins;
using RHttpServer.Response;

namespace RHttpServer
{
    internal sealed class CachePublicFileRequestHander : ResponseHandler
    {
        public CachePublicFileRequestHander(string publicDir, IFileCacheManager cache, RPluginCollection rplugins)
        {
            _pdir = publicDir;
            _cacheMan = cache;
            _rPluginCollection = rplugins;
        }

        private readonly IFileCacheManager _cacheMan;
        private readonly string _pdir;
        private readonly RPluginCollection _rPluginCollection;

        public override bool Handle(string route, HttpListenerContext context)
        {
            var range = context.Request.Headers["Range"];
            var rangeSet = false;
            int rangeStart = 0, rangeEnd = 0;
            if (!string.IsNullOrEmpty(range))
            {
                range = range.Replace("bytes=", "");
                GetRange(range, out rangeStart, out rangeEnd);
                rangeSet = true;
            }


            var publicFile = Path.Combine(_pdir, route);

            byte[] temp = null;
            if (_cacheMan.TryGetFile(publicFile, out temp))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, rangeStart, rangeEnd,
                        GetType(publicFile), publicFile);
                else
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, GetType(publicFile), publicFile);
                return true;
            }

            if (File.Exists(publicFile))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile, rangeStart, rangeEnd);
                else
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                _cacheMan.TryAddFile(publicFile);
                return true;
            }

            var pfiles = _indexFiles.Select(x => Path.Combine(publicFile, x)).ToList();
            if (
                !string.IsNullOrEmpty(publicFile = pfiles.FirstOrDefault(iFile => _cacheMan.TryGetFile(iFile, out temp))))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, rangeStart, rangeEnd,
                        GetType(publicFile), publicFile);
                else
                    new RResponse(context.Response, _rPluginCollection).SendBytes(temp, GetType(publicFile), publicFile);
                return true;
            }
            if (!string.IsNullOrEmpty(publicFile = pfiles.FirstOrDefault(File.Exists)))
            {
                if (rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile, rangeStart, rangeEnd);
                else
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                _cacheMan.TryAddFile(publicFile);
                return true;
            }
            return false;
        }
    }
}