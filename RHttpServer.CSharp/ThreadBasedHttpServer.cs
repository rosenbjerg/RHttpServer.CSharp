using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using RHttpServer.Logging;

namespace RHttpServer
{
    /// <summary>
    ///     Http server where request are handled by a predetermined number of threads </br>
    ///     Good for processing many small requests
    /// </summary>
    public sealed class ThreadBasedHttpServer : BaseHttpServer
    {
        /// <summary>
        /// </summary>
        /// <param name="port"></param>
        /// <param name="requestHandlerThreads">The amount of threads to process requests with</param>
        /// <param name="path">Path to use as public dir. Set to null or empty string if none wanted</param>
        /// <param name="throwExceptions">Whether exceptions should be suppressed and logged, or thrown</param>
        public ThreadBasedHttpServer(int port, int requestHandlerThreads, string path = "", bool throwExceptions = false)
            : base(port, path, throwExceptions)
        {
            _workers = new Thread[requestHandlerThreads];
            _queue = new ConcurrentQueue<HttpListenerContext>();
            _ready = new ManualResetEventSlim(false);
        }

        private readonly ConcurrentQueue<HttpListenerContext> _queue;
        private readonly ManualResetEventSlim _ready;
        private readonly Thread[] _workers;

        protected override void OnStart()
        {
            for (var i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker) {Name = $"ReqHandler #{i}"};
                _workers[i].Start();
            }
        }

        protected override void OnStop()
        {
            foreach (var worker in _workers)
                worker.Join(100);
        }

        protected override void ProcessContext(HttpListenerContext context)
        {
            _queue.Enqueue(context);
            _ready.Set();
        }

        private void Worker()
        {
            WaitHandle[] wait = {_ready.WaitHandle, _stop.WaitHandle};

            while (0 == WaitHandle.WaitAny(wait))
                try
                {
                    HttpListenerContext context;
                    if (!_queue.TryDequeue(out context))
                    {
                        _ready.Reset();
                        continue;
                    }
                    if (!SecurityOn || SecMan.HandleRequest(context.Request))
                        Process(context);
                }
                catch (Exception ex)
                {
                    if (ThrowExceptions) throw;
                    Logger.Log(ex);
                }
        }
    }
}