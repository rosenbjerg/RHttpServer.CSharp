using System;

namespace RPlugin.RHttpServer
{
    public class RHttpServerException : Exception
    {
        internal RHttpServerException(string msg) : base(msg)
        {
        }
    }
}