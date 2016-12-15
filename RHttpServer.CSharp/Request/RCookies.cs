using System;
using System.Net;
using RHttpServer.Logging;

namespace RHttpServer.Request
{
    /// <summary>
    ///     Ease-of-use wrapper for request cookies
    /// </summary>
    public sealed class RCookies
    {
        internal RCookies(CookieCollection cocol)
        {
            _cookies = cocol;
        }

        private readonly CookieCollection _cookies;

        /// <summary>
        ///     Returns the cookie with the given tag if any
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
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    return null;
                }
            }
        }
    }
}