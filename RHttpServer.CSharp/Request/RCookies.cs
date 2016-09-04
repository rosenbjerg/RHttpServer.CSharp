using System;
using System.Net;

namespace RHttpServer.Request
{
    /// <summary>
    ///     Ease-of-use wrapper for request cookies
    /// </summary>
    public class RCookies
    {
        internal RCookies(CookieCollection cocol)
        {
            _cookies = cocol;
        }

        private readonly CookieCollection _cookies;

        public Cookie this[string ssId]
        {
            get
            {
                try
                {
                    return _cookies[ssId];
                }
                catch (Exception e)
                {
#if DEBUG
                    Console.WriteLine(e);
#endif
                    return null;
                }
            }
        }
    }
}