using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RHttpServer.Response;

namespace RHttpServer.Plugins.Default
{
    /// <summary>
    ///     Renderer for pages using ecs tags ("ecs files")
    /// </summary>
    internal sealed class EcsPageRenderer : RPlugin, IPageRenderer
    {
        private IFileCacheManager _cacheMan => UsePlugin<IFileCacheManager>();

        private static string InternalRender(StringBuilder pageContent, RenderParams parameters, bool cacheOn,
            IFileCacheManager cache)
        {
            if (parameters != null)
                foreach (var parPair in parameters)
                    pageContent.Replace(parPair.Key, parPair.Value);
            var matches = Regex.Matches(pageContent.ToString(),
                @"(?i)<¤([a-z]:|.)?[\\\/\w]+.(html|ecs|js|css|txt)¤>",
                RegexOptions.Compiled);

            foreach (var match in matches)
            {
                var m = match.ToString().Trim('<', '>', '¤');
                var rm = $"<¤{m}¤>";
                MemoryStream mem;
                StringBuilder rfile;
                if (cacheOn && cache.TryGetFile(m, out mem))
                    rfile = new StringBuilder(Encoding.UTF8.GetString(mem.ToArray()));
                else if (File.Exists(m))
                {
                    var byt = File.ReadAllBytes(m);
                    rfile = new StringBuilder(Encoding.UTF8.GetString(byt));
                    if (cacheOn) cache.TryAdd(m, byt);
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
        ///     Whether the raw file should be cached so avoid file IO overhead
        /// </summary>
        public bool CachePages { get; set; }

        /// <summary>
        ///     Renders the ecs ecs file at the given path
        /// </summary>
        /// <param name="filepath">ecs file path</param>
        /// <param name="parameters">Rendering parameter</param>
        /// <returns></returns>
        public string Render(string filepath, RenderParams parameters)
        {
            if (!filepath.ToLowerInvariant().EndsWith(".ecs"))
                throw new RHttpServerException("Please use .ecs files when rendering pages");
            MemoryStream mem = null;
            StringBuilder sb;
            if (CachePages && _cacheMan.TryGetFile(filepath, out mem))
                sb = new StringBuilder(Encoding.UTF8.GetString(mem.ToArray()));
            else
            {
                var file = File.ReadAllBytes(filepath);
                sb = new StringBuilder(Encoding.UTF8.GetString(file));
                if (CachePages) _cacheMan.TryAdd(filepath, file);
            }
            InternalRender(sb, parameters, CachePages, _cacheMan);
            return sb.ToString();
        }

        /// <summary>
        ///     Applies ecs tag scheme to tag to prepare for rendering
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <returns>The </returns>
        public KeyValuePair<string, string> Parametrize(string tag, string data)
        {
            return new KeyValuePair<string, string>($"<%{tag.Trim(' ')}%>", data);
        }

        /// <summary>
        ///     Applies ecs tag scheme to tag to prepare for rendering.
        ///     Json formats the object, so it can be embedded as a js object
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