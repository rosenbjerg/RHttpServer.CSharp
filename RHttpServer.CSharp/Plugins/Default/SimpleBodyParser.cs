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
        private Type _stream = typeof(Stream);
        private Type _string = typeof(string);
        private Type _int = typeof(int);
        private Type _double = typeof(double);
        private Type _decimal = typeof(decimal);
        private Type _float = typeof(float);
        private Type _char = typeof(char);
        public async Task<T> ParseBody<T>(HttpListenerRequest underlyingRequest)
        {
            if (!underlyingRequest.HasEntityBody) return default(T);
            Type t = typeof(T);
            if (t == _stream) return (T)(object)underlyingRequest.InputStream;
            using (var stream = underlyingRequest.InputStream)
            {
                using (var reader = new StreamReader(stream, underlyingRequest.ContentEncoding))
                {
                    string txt;
                    if (t.IsPrimitive)
                    {
                        if (t == _string)
                        {
                            return (T)(object)await reader.ReadToEndAsync();
                        }
                        if (t == _int)
                        {
                            int i;
                            if (int.TryParse(await reader.ReadToEndAsync(), out i)) return (T)(object)i;
                            return default(T);
                        }
                        if (t == _double)
                        {
                            double d;
                            if (double.TryParse(await reader.ReadToEndAsync(), out d)) return (T)(object)d;
                            return default(T);
                        }
                        if (t == _decimal)
                        {
                            decimal d;
                            if (decimal.TryParse(await reader.ReadToEndAsync(), out d)) return (T)(object)d;
                            return default(T);
                        }
                        if (t == _float)
                        {
                            float f;
                            if (float.TryParse(await reader.ReadToEndAsync(), out f)) return (T)(object)f;
                            return default(T);
                        }
                        if (t == _char)
                        {
                            char c;
                            if (char.TryParse(await reader.ReadToEndAsync(), out c)) return (T)(object)c;
                            return default(T);
                        }
                    }
                    switch (underlyingRequest.ContentType)
                    {
                        case "application/xml":
                        case "text/xml":
                            txt = await reader.ReadToEndAsync();
                            reader.Dispose();
                            try
                            {
                                return UsePlugin<IXmlConverter>().Deserialize<T>(txt);
                            }
                            catch (FormatException ex)
                            {
                                Logger.Log(ex);
                                return default(T);
                            }
                        case "application/json":
                        case "text/json":
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
                            return default(T);
                    }
                }
            }
        }
    }
}