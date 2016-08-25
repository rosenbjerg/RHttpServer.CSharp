using System;
using System.IO;
using System.Text;

namespace WebServerHoster
{
    class Program
    {
        private static SimpleHttpServer _server;

        static void Main(string[] args)
        {
            var port = 3000;

            _server = new SimpleHttpServer("./public", port);

            _server.Get("/", (req, res) =>
            {
                res.SendString("Hello world");
            });

            _server.Get("/test", (req, res) =>
            {
                res.SendString("test");
            });

            Console.WriteLine("Server is running on this port: " + _server.Port + "\nPress any key to close");
            Console.ReadKey();
            _server.Stop();
        }
    }
}
