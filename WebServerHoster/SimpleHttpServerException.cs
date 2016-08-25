using System;

namespace WebServerHoster
{
    public class SimpleHttpServerException : Exception
    {
        public SimpleHttpServerException(string msg) : base(msg)
        {
            
        }
    }
}