using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RHttpServer.Plugins;

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
            Cookies = new RCookies(req.Cookies);
            Headers = new RHeaders(req.Headers);
            Queries = new RQueries(req.QueryString);
            _bodyParser = pluginCollection.Use<IBodyParser>();
        }

        /// <summary>
        ///     The query elements of the request
        /// </summary>
        public RQueries Queries { get; set; }

        private readonly IBodyParser _bodyParser;

        /// <summary>
        ///     The headers contained in the request
        /// </summary>
        public RHeaders Headers { get; set; }

        /// <summary>
        ///     The cookies contained in the request
        /// </summary>
        public RCookies Cookies { get; }

        /// <summary>
        ///     The url parameters of the request
        /// </summary>
        public RequestParams Params { get; }

        /// <summary>
        ///     The underlying HttpListenerRequest
        ///     This implementation of RRequest is leaky, to avoid limiting you
        /// </summary>
        public HttpListenerRequest UnderlyingRequest { get; }

        /// <summary>
        ///     Returns the body of the request
        ///     and null if the request does not contain a body
        /// </summary>
        /// <returns>The request body as a string</returns>
        public Task<T> GetBody<T>()
        {
            return _bodyParser.ParseBody<T>(UnderlyingRequest);
        }

        /// <summary>
        ///     Save the request body, if any, to a file in the specified directory <para />
        ///     If no filename is provided, the file will be named according to the Content-Disposition header (if any)<para />
        ///     Will get random filename if no Content-Dispositon header is found
        /// </summary>
        /// <param name="filePath">The directory to placed to file in</param>
        /// <param name="fileName">The filename to use.</param>
        /// <returns>Whether the file was succesfully saved</returns>
        public async Task<bool> SaveBodyToFile(string filePath, string fileName = "")
        {
            if (!UnderlyingRequest.HasEntityBody) return false;
            string fullFilePath = "";
            if (string.IsNullOrWhiteSpace(fileName))
            {
                var f = Headers["Content-Disposition"].Split(new [] {';'}, StringSplitOptions.RemoveEmptyEntries);
                var i = f.FirstOrDefault(s => s.Contains("filename"));
                fullFilePath = Path.Combine(filePath, i?.Replace("filename=", "").Trim(' ', '\"') ?? Path.GetRandomFileName());
            }
            else fullFilePath = Path.Combine(filePath, fileName);
            try
            {
                using (var stream = UnderlyingRequest.InputStream)
                {
                    using (var file = File.Create(fullFilePath))
                    {
                        var buffer = new byte[0x4000];

                        int nbytes;
                        while ((nbytes = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            await file.WriteAsync(buffer, 0, nbytes);
                        }
                        file.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Logger.Log(e);
                return false;
            }
            return true;
            
        }
    }
}