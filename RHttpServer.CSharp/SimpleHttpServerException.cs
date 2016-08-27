using System;

namespace RHttpServer
{
    internal class SimpleHttpServerException : Exception
    {
        public SimpleHttpServerException(string msg) : base(msg)
        {
            
        }
    }
}