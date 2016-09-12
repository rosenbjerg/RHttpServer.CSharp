using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RHttpServer.Response;

namespace RHttpServer.Plugins.Default
{
    /// <summary>
    /// Renderer for pages using ecs tags ("ecs files")
    /// </summary>
    internal sealed class EcsPageRenderer : RPlugin, IPageRenderer
    {
        private readonly ConcurrentDictionary<string, string> _cachedPages = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Whether the raw file should be cached so avoid file IO overhead
        /// </summary>
        public bool CachePages { get; set; }

        /// <summary>
        /// Clears the cache
        /// </summary>
        public void EmptyCache()
        {
            _cachedPages.Clear();
        }

        /// <summary>
        /// Renders the ecs ecs file at the given path
        /// </summary>
        /// <param name="filepath">ecs file path</param>
        /// <param name="parameters">Rendering parameter</param>
        /// <returns></returns>
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
            foreach (var parPair in parameters)
            {
                pageContent.Replace(parPair.Key, parPair.Value);
            }
            var matches = Regex.Matches(pageContent.ToString(),
                @"(?i)<¤([a-z]:|.)?[\\\/\w]+.(html|ecs|js|css|txt)¤>", 
                RegexOptions.Compiled);

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

        /// <summary>
        /// Applies ecs tag scheme to tag to prepare for rendering
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <returns>The </returns>
        public KeyValuePair<string, string> Parametrize(string tag, string data)
        {
            return new KeyValuePair<string, string>($"<%{tag.Trim(' ')}%>", data);
        }

        /// <summary>
        /// Applies ecs tag scheme to tag to prepare for rendering.
        /// Json formats the object, so it can be embedded as a js object
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> ParametrizeObject(string tag, object data)
        {
            return new KeyValuePair<string, string>($"<%{tag.Trim(' ')}%>", UsePlugin<IJsonConverter>().Serialize(data));
        }
    }
}