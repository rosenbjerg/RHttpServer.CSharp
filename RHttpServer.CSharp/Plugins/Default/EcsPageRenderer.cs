using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RHttpServer.Response;

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
            if (!filepath.ToLowerInvariant().EndsWith(".ecs"))
                throw new RHttpServerException("Please use .ecs files when rendering pages");
            var file = "";
            StringBuilder sb;
            if (CachePages && _cachedPages.TryGetValue(filepath, out file))
                sb = new StringBuilder(file);
            else
            {
                sb = new StringBuilder(File.ReadAllText(filepath));
                if (CachePages) _cachedPages.TryAdd(filepath, sb.ToString());
            }
            InternalRender(sb, parameters, CachePages, _cachedPages);
            return sb.ToString();
        }

        private static string InternalRender(StringBuilder pageContent, RenderParams parameters, bool cacheOn, ConcurrentDictionary<string, string> cache)
        {
            //Parallel.ForEach(parameters, (p) =>
            //{
            //    pageContent.Replace(p.Key, p.Value);
            //});
            foreach (var parPair in parameters)
            {
                pageContent.Replace(parPair.Key, parPair.Value);
            }
            var matches = Regex.Matches(pageContent.ToString(),
                @"(?i)<¤([a-z]:|.)?[\\\/\w]+.(html|ecs|js|css|txt)¤>", 
                RegexOptions.Compiled);
            //Parallel.ForEach(matches.OfType<Match>(), (match) =>
            //{
            //    var m = match.ToString().Trim('<', '>', '¤');
            //    var rm = $"<¤{m}¤>";
            //    var file = "";
            //    StringBuilder rfile;
            //    if (cacheOn && cache.TryGetValue(m, out file))
            //        rfile = new StringBuilder(file);
            //    else if (File.Exists(m))
            //    {
            //        rfile = new StringBuilder(File.ReadAllText(m, Encoding.UTF8));
            //        if (cacheOn) cache.TryAdd(m, rfile.ToString());
            //    }
            //    else return;

            //    pageContent.Replace(rm,
            //        m.ToLowerInvariant().EndsWith(".ecs")
            //            ? InternalRender(rfile, parameters, cacheOn, cache)
            //            : rfile.ToString());
            //});

            foreach (var match in matches)
            {
                var m = match.ToString().Trim('<', '>', '¤');
                var rm = $"<¤{m}¤>";
                var file = "";
                StringBuilder rfile;
                if (cacheOn && cache.TryGetValue(m, out file))
                    rfile = new StringBuilder(file);
                else if (File.Exists(m))
                {
                    rfile = new StringBuilder(File.ReadAllText(m, Encoding.UTF8));
                    if (cacheOn) cache.TryAdd(m, rfile.ToString());
                }
                else continue;

                pageContent.Replace(rm,
                    m.ToLowerInvariant().EndsWith(".ecs")
                        ? InternalRender(rfile, parameters, cacheOn, cache)
                        : rfile.ToString());
            }
            return pageContent.ToString();
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