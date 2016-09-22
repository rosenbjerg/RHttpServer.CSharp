using System.Collections.Specialized;
using System.Linq;

namespace RHttpServer.Request
{
    /// <summary>
    ///     Ease-of-use wrapper for request queries
    /// </summary>
    public class RQueries
    {
        private readonly NameValueCollection _qString;
        private readonly int _len;

        internal RQueries(NameValueCollection queryString)
        {
            _qString = queryString;
            _len = queryString.AllKeys.Length;
        }

        /// <summary>
        /// Tries to retrieve the value at index of query.
        /// Returns empty string if out of range of query
        /// </summary>
        /// <param name="index">The index of the query to retrieve from</param>
        public string this[int index]
        {
            get { return index < _len ? _qString[index] : ""; }
        }

        /// <summary>
        /// Tries to retrieve the value of the query tag.
        /// Returns empty string if not found
        /// </summary>
        /// <param name="tag">The id of the tag in the query</param>
        public object this[string tag]
        {
            get { return _qString.AllKeys.Contains(tag) ? _qString[tag] : ""; }
        }
    }
}