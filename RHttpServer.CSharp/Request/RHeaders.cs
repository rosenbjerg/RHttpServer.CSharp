using System.Collections.Specialized;
using System.Linq;

namespace RHttpServer.Request
{
    /// <summary>
    ///     Ease-of-use wrapper for request headers
    /// </summary>
    public class RHeaders
    {
        internal RHeaders(NameValueCollection headers)
        {
            _headers = headers;
        }

        private readonly NameValueCollection _headers;

        /// <summary>
        ///     Tries to retrieve the content of a given header
        /// </summary>
        /// <param name="headerName"></param>
        /// <returns></returns>
        public string this[string headerName]
        {
            get
            {
                if (_headers.AllKeys.Any(h => h == headerName)) return null;
                return _headers[headerName];
            }
        }
    }
}