using System.Collections.Generic;

namespace RHttpServer.Request
{
    /// <summary>
    ///     Object containing parameters for a request
    /// </summary>
    public class RequestParams
    {
        internal RequestParams(Dictionary<string, string> dict)
        {
            _dict = dict;
        }

        private readonly Dictionary<string, string> _dict;

        /// <summary>
        ///     Get the request data fora given parameter
        /// </summary>
        /// <param name="paramId"></param>
        /// <returns></returns>
        public string this[string paramId]
        {
            get
            {
                if (_dict == null) return "";
                var v = "";
                _dict.TryGetValue(paramId, out v);
                return v;
            }
        }
    }
}