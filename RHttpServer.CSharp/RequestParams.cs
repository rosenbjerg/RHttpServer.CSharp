using System.Collections.Generic;

namespace RHttpServer
{
    /// <summary>
    /// Object containing parameters for a request
    /// </summary>
    public class RequestParams
    {
        private readonly Dictionary<string, string> _dict;

        public RequestParams(Dictionary<string, string> dict)
        {
            _dict = dict;
        }
        /// <summary>
        /// Get the request data fora given parameter
        /// </summary>
        /// <param name="paramId"></param>
        /// <returns></returns>
        public string this[string paramId]
        {
            get
            {
                if (_dict == null) return null;
                string v = null;
                _dict.TryGetValue(paramId, out v);
                return v;
            }
        }
    }
}