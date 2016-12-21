using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using RHttpServer.Response;

namespace RHttpServer.Plugins.Default
{
    /// <summary>
    ///     Renderer for pages using ecs tags ("ecs files")
    /// </summary>
    internal sealed class EcsPageRenderer : RPlugin, IPageRenderer
    {
        private static readonly Regex NormalTagRegex = new Regex(@"(?i)<% ?[a-z_][a-z_0-9]* ?%>", RegexOptions.Compiled);
        private static readonly Regex HtmlTagRegex = new Regex(@"(?i)<%= ?[a-z_][a-z_0-9]* ?=%>", RegexOptions.Compiled);

        private static readonly Regex FileTagRegex = new Regex(
            @"(?i)<¤ ?([a-z]:|.)?[\\\/\w]+.(html|ecs|js|css|txt) ?¤>", RegexOptions.Compiled);

        private IFileCacheManager _cacheMan;
        private IFileCacheManager CacheMan => _cacheMan ?? (_cacheMan = UsePlugin<IFileCacheManager>());

        private static string InternalRender(StringBuilder pageContent, RenderParams parameters, bool cacheOn,
            IFileCacheManager cache)
        {
            var doneHashset = new HashSet<Match>();
            if (parameters != null)
            {
                var pmatches = NormalTagRegex.Matches(pageContent.ToString());
                foreach (Match pmatch in pmatches)
                {
                    if (doneHashset.Contains(pmatch)) continue;
                    var m = pmatch.Value.Trim('<', '>', '%', ' ');
                    var t = parameters[m];
                    if (t == null) continue;
                    pageContent.Replace(pmatch.Value, t);
                    doneHashset.Add(pmatch);
                }
                pmatches = HtmlTagRegex.Matches(pageContent.ToString());
                foreach (Match pmatch in pmatches)
                {
                    if (doneHashset.Contains(pmatch)) continue;
                    var m = pmatch.Value.Trim('<', '>', '%', '=', ' ');
                    var t = parameters[m];
                    if (string.IsNullOrEmpty(t)) continue;
                    pageContent.Replace(pmatch.Value, HttpUtility.HtmlEncode(t));
                    doneHashset.Add(pmatch);
                }
            }


            var matches = FileTagRegex.Matches(pageContent.ToString());
            foreach (Match match in matches)
            {
                if (doneHashset.Contains(match)) continue;
                var m = match.Value.Trim('<', '>', '¤', ' ');
                byte[] data;
                StringBuilder rfile;
                if (cacheOn && cache.TryGetFile(m, out data))
                    rfile = new StringBuilder(Encoding.UTF8.GetString(data));
                else if (File.Exists(m))
                {
                    data = File.ReadAllBytes(m);
                    rfile = new StringBuilder(Encoding.UTF8.GetString(data));
                    if (cacheOn) cache.TryAdd(m, data);
                }
                else continue;

                pageContent.Replace(match.Value,
                    Path.GetExtension(m) == ".ecs"
                        ? InternalRender(rfile, parameters, cacheOn, cache)
                        : rfile.ToString());
                doneHashset.Add(match);
            }
            return pageContent.ToString();
        }

        /// <summary>
        ///     Whether the raw file should be cached to avoid file IO overhead
        /// </summary>
        public bool CachePages { get; set; }

        /// <summary>
        ///     Renders the ecs file at the given path
        /// </summary>
        /// <param name="filepath">ecs file path</param>
        /// <param name="parameters">Rendering parameter</param>
        /// <returns></returns>
        public string Render(string filepath, RenderParams parameters)
        {
            if (Path.GetExtension(filepath) != ".ecs")
                throw new RHttpServerException("Please use .ecs files for rendering");
            byte[] data;
            StringBuilder sb;
            if (CachePages && CacheMan.TryGetFile(filepath, out data))
                sb = new StringBuilder(Encoding.UTF8.GetString(data));
            else
            {
                var file = File.ReadAllBytes(filepath);
                sb = new StringBuilder(Encoding.UTF8.GetString(file));
                if (CachePages) CacheMan.TryAdd(filepath, file);
            }
            InternalRender(sb, parameters, CachePages, CacheMan);
            return sb.ToString();
        }

        //{
        //public KeyValuePair<string, string> Parametrize(string tag, string data)
        ///// <returns>The </returns>
        ///// <param name="data"></param>
        ///// <param name="tag"></param>
        ///// </summary>
        /////     Applies ecs tag scheme to tag to prepare for rendering

        ///// <summary>
        //    return new KeyValuePair<string, string>($"<%{tag.Trim(' ')}%>", data);
        //}

        ///// <summary>
        /////     Applies ecs html tag scheme to tag to prepare for rendering
        ///// </summary>
        ///// <param name="tag"></param>
        ///// <param name="data"></param>
        ///// <returns>The </returns>
        //public KeyValuePair<string, string> HtmlParametrize(string tag, string data)
        //{
        //    return new KeyValuePair<string, string>($"<%={tag.Trim(' ')}=%>", _jsonMan.Serialize(data));
        //}

        ///// <summary>
        /////     Applies ecs tag scheme to tag to prepare for rendering.
        /////     Json formats the object, so it can be embedded as a js object
        ///// </summary>
        ///// <param name="tag"></param>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //public KeyValuePair<string, string> ParametrizeObject(string tag, object data)
        //{
        //    return new KeyValuePair<string, string>($"<%{tag.Trim(' ')}%>", _jsonMan.Serialize(data));
        //}
    }
}