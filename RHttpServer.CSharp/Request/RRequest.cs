using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RHttpServer.Plugins;
using RHttpServer.Request.MultiPartFormParsing;

namespace RHttpServer.Request
{
    /// <summary>
    ///     Class representing a request from a client
    /// </summary>
    public class RRequest
    {
        internal RRequest(HttpListenerRequest req, RequestParams par, RPluginCollection pluginCollection)
        {
            UnderlyingRequest = req;
            Params = par;
            _bodyParser = pluginCollection.Use<IBodyParser>();
        }

        private readonly IBodyParser _bodyParser;

        private RCookies _cookies;

        private RHeaders _headers;

        private RQueries _queries;

        /// <summary>
        ///     The query elements of the request
        /// </summary>
        public RQueries Queries => _queries ?? (_queries = new RQueries(UnderlyingRequest.QueryString));

        /// <summary>
        ///     The headers contained in the request
        /// </summary>
        public RHeaders Headers => _headers ?? (_headers = new RHeaders(UnderlyingRequest.Headers));

        /// <summary>
        ///     The cookies contained in the request
        /// </summary>
        public RCookies Cookies => _cookies ?? (_cookies = new RCookies(UnderlyingRequest.Cookies));

        /// <summary>
        ///     The url parameters of the request
        /// </summary>
        public RequestParams Params { get; }

        /// <summary>
        ///     The underlying HttpListenerRequest <para/>
        ///     The implementation of RRequest is leaky, to avoid limiting you
        /// </summary>
        public HttpListenerRequest UnderlyingRequest { get; }
        


        /// <summary>
        ///     Returns the body stream of the request
        ///     and null if the request does not contain a body
        /// </summary>
        /// <returns></returns>
        public Stream GetBodyStream()
        {
            if (UnderlyingRequest.HasEntityBody || UnderlyingRequest.InputStream == Stream.Null) return null;
            return UnderlyingRequest.InputStream;
        }

        /// <summary>
        ///     Returns the body deserialized or parsed to specified type, if possible, default if not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ParseBody<T>()
        {
            return _bodyParser.ParseBody<T>(UnderlyingRequest);
        }

        /// <summary>
        ///     Returns the body deserialized or parsed to specified type, if possible, default if not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> ParseBodyAsync<T>()
        {
            return await _bodyParser.ParseBodyAsync<T>(UnderlyingRequest);
        }

        /// <summary>
        ///     Returns form-data from post request, if any
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetBodyPostFormData()
        {
            if (!UnderlyingRequest.ContentType.Contains("x-www-form-urlencoded"))
                return new Dictionary<string, string>();
            using (var reader = new StreamReader(UnderlyingRequest.InputStream))
            {
                var txt = reader.ReadToEnd();
                var pars = txt.Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries);
                return pars.Select(par => par.Split('=')).ToDictionary(split => split[0], split => split[1]);
            }
        }

        /// <summary>
        ///     Save multipart form-data from request body, if any, to a file in the specified directory
        /// </summary>
        /// <param name="filePath">The directory to placed to file in</param>
        /// <param name="filerenamer">Function to rename the file(s)</param>
        /// <returns>Whether the file was saved succesfully</returns>
        public bool SaveBodyToFile(string filePath, Func<string, string> filerenamer = null)
        {
            if (!UnderlyingRequest.HasEntityBody) return false;
            var filestreams = new Dictionary<string, Stream>();

            var parser = new StreamingMultipartFormDataParser(UnderlyingRequest.InputStream);
            var files = new List<string>();
            parser.FileHandler += (name, fname, type, disposition, buffer, bytes) =>
            {
                if (filerenamer != null) fname = filerenamer(fname);
                Stream stream;
                if (!filestreams.TryGetValue(name, out stream))
                {
                    var fullFilePath = Path.Combine(filePath, fname);
                    stream = File.Create(fullFilePath);
                    filestreams.Add(name, stream);
                    files.Add(name);
                }
                stream.Write(buffer, 0, bytes);
                stream.Flush();
            };
            parser.StreamClosedHandler += () =>
            {
                foreach (var file in files)
                    filestreams[file].Close();
            };
            try
            {
                parser.Run();
            }
            catch (MultipartParseException)
            {
                return false;
            }
            return true;
        }
    }
}