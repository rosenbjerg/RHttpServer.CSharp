using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace WebServerHoster
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = 3000;

            var server = new SimpleHttpServer("./public", port);

            server.Get("/", (req, res) =>
            {
                res.SendFile("./public/index.html");
            });

            server.Get("/*", (req, res) =>
            {
                res.SendString("404 - Nothing found man");
            });

            //server.Get("/:testvar", (req, res) =>
            //{
            //    var test = req.Params["testvar"];
            //    res.SendString("You wrote: " + test);
            //});

            server.Get("/test", (req, res) =>
            {
                res.SendString("test");
            });

            //server.Get("/test2", (req, res) =>
            //{
            //    var pars = new RenderParams
            //    {
            //        { "data1", "data data data data ..." },
            //        { "data2", "test test test test."}
            //    };
            //    res.RenderPage("./public/index.ecs", pars);
            //});

            server.Start();
            Console.WriteLine("\nPress any key to close");
            Console.ReadKey();
            server.Stop();
        }
    }
}
