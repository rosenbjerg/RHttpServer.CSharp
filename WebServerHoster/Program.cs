using System;
using RHttpServer;
using RHttpServer.Plugins;

namespace WebServerHoster
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = 3000;

            var server = new SimpleHttpServer("./public", port, 3);


            server.Get("/", (req, res) =>
            {
                res.SendString("Welcome");
            });

            server.Get("/file", (req, res) =>
            {
                res.SendFile("./public/index.html");
            });

            server.Get("/render", (req, res) =>
            {
                var pars = server.CreateRenderParams();
                pars.Add("data1", "test1");
                pars.Add("data2", "{\"Test\":\"test2\"}");
                res.RenderPage("./public/index.ecs", pars);
            });
            
            server.Get("/:test", (req, res) =>
            {
                var pars = server.CreateRenderParams();
                pars.Add("data1", req.Params["test"]);
                pars.Add("data2", 42);

                res.RenderPage("./public/index.ecs", pars);
            });

            server.Get("/404", (req, res) =>
            {
                res.SendString("404 - Nothing found");
            });

            server.Get("/*", (req, res) =>
            {
                res.Redirect("/404");
            });

            server.SetSecuritySettings(false);

            server.AddPlugin<IJsonConverter, SimpleNewtonsoftJsonConverter>(new SimpleNewtonsoftJsonConverter());

            server.Start(true);
            Console.WriteLine("\nPress any key to close");
            Console.ReadKey();
            server.Stop();
        }
    }
}
