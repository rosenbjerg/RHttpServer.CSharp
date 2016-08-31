using System;

namespace RHttpServer.Core
{
    internal class RHttpServerException : Exception
    {
        public RHttpServerException(string msg) : base(msg)
        {
            
        }
    }
}