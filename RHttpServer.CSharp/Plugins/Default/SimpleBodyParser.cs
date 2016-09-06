using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace RHttpServer.Plugins.Default
{
    internal sealed class SimpleBodyParser : RPlugin, IBodyParser
    {
        public async Task<T> ParseBody<T>(HttpListenerRequest underlyingRequest)
        {
            if (!underlyingRequest.HasEntityBody) return default(T);
            using (var stream = underlyingRequest.InputStream)
            {
                using (var reader = new StreamReader(stream, underlyingRequest.ContentEncoding))
                {
                    string txt;
                    switch (underlyingRequest.ContentType)
                    {
                        case "application/json":
                            txt = await reader.ReadToEndAsync();
                            reader.Dispose();
                            return UsePlugin<IJsonConverter>().Deserialize<T>(txt);
                        default:
                            txt = await reader.ReadToEndAsync();
                            reader.Dispose();
                            return (T) (object) txt;
                    }
                }
            }
        }
    }
}