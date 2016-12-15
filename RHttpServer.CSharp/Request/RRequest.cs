using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
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
            _rp = pluginCollection;
        }
        
        private RCookies _cookies;
        private RHeaders _headers;
        private RQueries _queries;
        private readonly RPluginCollection _rp;

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
        ///     The underlying HttpListenerRequest
        ///     <para />
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
            if (UnderlyingRequest.HasEntityBody || (UnderlyingRequest.InputStream == Stream.Null)) return null;
            return UnderlyingRequest.InputStream;
        }

        /// <summary>
        ///     Returns the body deserialized or parsed to specified type, if possible, default if not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ParseBody<T>()
        {
            return _rp.Use<IBodyParser>().ParseBody<T>(UnderlyingRequest);
        }

        /// <summary>
        ///     Returns the body deserialized or parsed to specified type, if possible, default if not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> ParseBodyAsync<T>()
        {
            return await _rp.Use<IBodyParser>().ParseBodyAsync<T>(UnderlyingRequest);
        }

        /// <summary>
        ///     Returns form-data from post request, if any
        /// </summary>
        /// <returns></returns>
        public NameValueCollection GetBodyPostFormData()
        {
            if (!UnderlyingRequest.ContentType.Contains("x-www-form-urlencoded"))
                return new NameValueCollection();
            using (var reader = new StreamReader(UnderlyingRequest.InputStream))
            {
                var txt = reader.ReadToEnd();
                return HttpUtility.ParseQueryString(txt);
            }
        }

        /// <summary>
        ///     Save multipart form-data from request body, if any, to a file in the specified directory
        /// </summary>
        /// <param name="filePath">The directory to placed to file in</param>
        /// <param name="filerenamer">Function to rename the file(s)</param>
        /// <param name="maxSizeKb">The max filesize allowed</param>
        /// <returns>Whether the file was saved succesfully</returns>
        public Task<bool> SaveBodyToFile(string filePath, Func<string, string> filerenamer = null, long maxSizeKb = 1000)
        {
            maxSizeKb = maxSizeKb << 6;
            var tcs = new TaskCompletionSource<bool>();
            if (!UnderlyingRequest.HasEntityBody) return Task.FromResult(false);
            var filestreams = new Dictionary<string, Stream>();

            var parser = new StreamingMultipartFormDataParser(UnderlyingRequest.InputStream);
            var files = new List<string>();
            parser.FileHandler += async (name, fname, type, disposition, buffer, bytes) =>
            {
                if (bytes > maxSizeKb)
                {
                    tcs.TrySetResult(false);
                    return;
                }
                if (filerenamer != null) fname = filerenamer(fname);
                Stream stream;
                if (!filestreams.TryGetValue(name, out stream))
                {
                    var fullFilePath = Path.Combine(filePath, fname);
                    stream = File.Create(fullFilePath);
                    filestreams.Add(name, stream);
                    files.Add(name);
                }
                await stream.WriteAsync(buffer, 0, bytes);
                await stream.FlushAsync();
                tcs.TrySetResult(true);
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
            catch (MultipartParseException ex)
            {
                tcs.TrySetException(ex);
            }
            return tcs.Task;
        }
    }
}