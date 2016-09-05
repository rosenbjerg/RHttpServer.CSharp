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

        /// <summary>
        /// Returns the cookie with the given id if any
        /// </summary>
        /// <param name="cookieId"></param>
        public Cookie this[string cookieId]
        {
            get
            {
                try
                {
                    return _cookies[cookieId];
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