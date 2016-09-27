// Taken from https://github.com/Vodurden/Http-Multipart-Data-Parser by Jake Woods
using System;

namespace RHttpServer.Request.MultiPartFormParsing
{
    internal class MultipartParseException : Exception
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultipartParseException" /> class.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        public MultipartParseException(string message)
            : base(message)
        {
        }

        #endregion
    }
}