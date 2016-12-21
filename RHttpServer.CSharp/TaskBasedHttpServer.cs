using System.Net;
using System.Threading.Tasks;

namespace RHttpServer
{
    public sealed class TaskBasedHttpServer : BaseHttpServer
    {
        /// <summary>
        ///     Http server where request are handled as tasks </br>
        ///     Good for processing requests of varying size
        /// </summary>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="port">The port that the server should listen on</param>
        /// <param name="throwExceptions">Whether exceptions should be suppressed and logged, or thrown</param>
        public TaskBasedHttpServer(int port, string path = "", bool throwExceptions = false)
            : base(port, path, throwExceptions)
        {
        }


        protected override void ProcessContext(HttpListenerContext context)
        {
            Task.Run(() =>
            {
                if (!SecurityOn || SecMan.HandleRequest(context.Request))
                    Process(context);
            });
        }
    }
}