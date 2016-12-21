using System.IO;
using System.Linq;
using System.Net;
using RHttpServer.Plugins;
using RHttpServer.Response;

namespace RHttpServer
{
    internal sealed class PublicFileRequestHander : ResponseHandler
    {
        public PublicFileRequestHander(string publicDir, RPluginCollection rplugins)
        {
            _pdir = publicDir;
            _rPluginCollection = rplugins;
        }

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

            if (File.Exists(publicFile))
            {
                if (!rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                else
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile, rangeStart, rangeEnd);
                return true;
            }

            var pfiles = _indexFiles.Select(x => Path.Combine(publicFile, x));
            if (!string.IsNullOrEmpty(publicFile = pfiles.FirstOrDefault(File.Exists)))
            {
                if (!rangeSet)
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile);
                else
                    new RResponse(context.Response, _rPluginCollection).SendFile(publicFile, rangeStart, rangeEnd);
                return true;
            }
            return false;
        }
    }
}