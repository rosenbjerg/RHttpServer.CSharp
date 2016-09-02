using System.Collections.Concurrent;
using System.Collections.Generic;
using RHttpServer.Core;
using RHttpServer.Core.Response;

namespace RHttpServer.Plugins.Default
{
    internal sealed class EcsPageRenderer : RPlugin, IPageRenderer
    {
        private readonly ConcurrentDictionary<string, string> _cachedPages = new ConcurrentDictionary<string, string>();

        public bool CachePages { get; set; }

        public void EmptyCache()
        {
            _cachedPages.Clear();
        }

        public string Render(string filepath, RenderParams parameters)
        {
            if (!filepath.ToLowerInvariant().EndsWith(".ecs")) throw new RHttpServerException("Please use .ecs files when rendering pages");
            string file = "";
            var sb = new System.Text.StringBuilder();
            if (CachePages && _cachedPages.TryGetValue(filepath, out file))
                sb = new System.Text.StringBuilder(file);
            else
            {
                sb = new System.Text.StringBuilder(System.IO.File.ReadAllText(filepath));
                if (CachePages) _cachedPages.TryAdd(filepath, sb.ToString());
            }
            foreach (var parPair in parameters)
            {
                sb.Replace(parPair.Key, parPair.Value);
            }
            return sb.ToString();
        }

        public KeyValuePair<string, string> Parametrize(string tag, string data)
        {
            return new KeyValuePair<string, string>($"<%{tag.Trim(' ')}%>", data);
        }

        public KeyValuePair<string, string> ParametrizeObject(string tag, object data)
        {
            return new KeyValuePair<string, string>($"<%{tag.Trim(' ')}%>", UsePlugin<IJsonConverter>().Serialize(data));
        }
    }
}