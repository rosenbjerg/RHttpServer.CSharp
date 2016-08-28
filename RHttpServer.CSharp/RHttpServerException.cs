using System;

namespace RHttpServer
{
    internal class RHttpServerException : Exception
    {
        public RHttpServerException(string msg) : base(msg)
        {
            
        }
    }
}