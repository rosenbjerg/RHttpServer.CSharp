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

            server.Get("/*", (req, res) =>
            {
                res.SendString("404 - Nothing found man");
            });

            //server.Get("/:testvar", (req, res) =>
            //{
            //    var test = req.Params["testvar"];
            //    res.SendString("You wrote: " + test);
            //});

            server.Get("/1", (req, res) =>
            {
                res.SendString("1");
            });

            server.Get("/1/*", (req, res) =>
            {
                res.SendString("1*");
            });

            server.Get("/1/2/:test/:test2", (req, res) =>
            {
                var pars = new RenderParams
                    {
                        { "data1", req.Params["test"] },
                        { "data2", "{\"test\":" + req.Params["test2"] + "}" }
                    };
                res.RenderPage("./public/index.ecs", pars);
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
