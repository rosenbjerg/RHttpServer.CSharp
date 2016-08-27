using System;

namespace RHttpServer
{
    public class SimpleHttpServerException : Exception
    {
        public SimpleHttpServerException(string msg) : base(msg)
        {
            
        }
    }
}