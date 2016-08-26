using System.Collections.Generic;

namespace WebServerHoster
{
    public class RequestParams
    {
        private readonly Dictionary<string, string> _dict;

        public RequestParams(Dictionary<string, string> dict)
        {
            _dict = dict;
        }
        
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