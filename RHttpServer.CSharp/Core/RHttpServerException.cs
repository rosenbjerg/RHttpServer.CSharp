using System;

namespace RHttpServer.Core
{
    public class RHttpServerException : Exception
    {
        internal RHttpServerException(string msg) : base(msg)
        {
            
        }
    }
}