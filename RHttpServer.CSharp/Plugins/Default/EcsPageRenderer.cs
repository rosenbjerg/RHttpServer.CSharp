using System.Collections.Generic;
using RHttpServer.Response;

namespace RHttpServer.Plugins.Default
{
    internal sealed class EcsPageRenderer : RPlugin, IPageRenderer
    {
        public string Render(string filepath, RenderParams parameters)
        {
            if (!filepath.ToLowerInvariant().EndsWith(".ecs")) throw new RHttpServerException("Please use .ecs files when rendering pages");
            var sb = new System.Text.StringBuilder(System.IO.File.ReadAllText(filepath));
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