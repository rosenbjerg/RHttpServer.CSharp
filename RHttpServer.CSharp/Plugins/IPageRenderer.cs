using System.Collections.Generic;
using RHttpServer.Core.Response;

namespace RHttpServer.Plugins
{
    public interface IPageRenderer
    {
        bool CachePages { get; set; }
        void EmptyCache();
        string Render(string filepath, RenderParams parameters);
        KeyValuePair<string, string> Parametrize(string tag, string data);
        KeyValuePair<string, string> ParametrizeObject(string tag, object data);
    }
}