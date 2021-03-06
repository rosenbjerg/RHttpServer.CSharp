using System.Net;
using System.Threading.Tasks;

namespace RHttpServer
{
    /// <summary>
    ///     Interface for classes used to parse the request body data stream
    /// </summary>
    public interface IBodyParser
    {
        /// <summary>
        ///     The method that must handle the body stream async
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<T> ParseBodyAsync<T>(HttpListenerRequest underlyingRequest);

        /// <summary>
        ///     The method that must handle the body stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T ParseBody<T>(HttpListenerRequest underlyingRequest);
    }
}