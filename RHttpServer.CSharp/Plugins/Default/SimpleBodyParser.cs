using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using RHttpServer.Logging;

namespace RHttpServer.Plugins.Default
{
    /// <summary>
    /// Simple body parser that can be used to parse JSON objects and C# primitives, or just return the input stream
    /// </summary>
    internal sealed class SimpleBodyParser : RPlugin, IBodyParser
    {
        public async Task<T> ParseBody<T>(HttpListenerRequest underlyingRequest)
        {
            if (!underlyingRequest.HasEntityBody) return default(T);
            Type t = typeof(T);
            if (t == typeof(Stream)) return (T)(object)underlyingRequest.InputStream;
            using (var stream = underlyingRequest.InputStream)
            {
                using (var reader = new StreamReader(stream, underlyingRequest.ContentEncoding))
                {
                    string txt;
                    if (t.IsPrimitive)
                    {
                        if (t == typeof(string))
                        {
                            return (T)(object)await reader.ReadToEndAsync();
                        }
                        if (t == typeof(int))
                        {
                            int i;
                            if (int.TryParse(await reader.ReadToEndAsync(), out i)) return (T)(object)i;
                            return default(T);
                        }
                        if (t == typeof(double))
                        {
                            double d;
                            if (double.TryParse(await reader.ReadToEndAsync(), out d)) return (T)(object)d;
                            return default(T);
                        }
                        if (t == typeof(decimal))
                        {
                            decimal d;
                            if (decimal.TryParse(await reader.ReadToEndAsync(), out d)) return (T)(object)d;
                            return default(T);
                        }
                        if (t == typeof(float))
                        {
                            float f;
                            if (float.TryParse(await reader.ReadToEndAsync(), out f)) return (T)(object)f;
                            return default(T);
                        }
                        if (t == typeof(char))
                        {
                            char c;
                            if (char.TryParse(await reader.ReadToEndAsync(), out c)) return (T)(object)c;
                            return default(T);
                        }
                    }
                    switch (underlyingRequest.ContentType)
                    {
                        case "application/json":
                            txt = await reader.ReadToEndAsync();
                            reader.Dispose();
                            try
                            {
                                return UsePlugin<IJsonConverter>().Deserialize<T>(txt);
                            }
                            catch (FormatException ex)
                            {
                                Logger.Log(ex);
                                return default(T);
                            }
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