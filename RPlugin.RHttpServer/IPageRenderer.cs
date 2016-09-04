using System.Collections.Generic;

namespace RPlugin.RHttpServer
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