using System;
using RHttpServer.Plugins.Default;

namespace SimpleRHttpServer
{
    class Server
    {
        static void Main(string[] args)
        {
            var server = new RHttpServer.Core.HttpServer(3000, 3, "./public");

            server.Get("/", (req, res) =>
            {
                res.SendString("ok");
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

            server.Get("/:test/:test2", (req, res) =>
            {
                var pars = server.CreateRenderParams();
                pars.Add("data1", req.Params["test"]);
                pars.Add("data2", req.Params["test2"]);

                res.RenderPage("./public/index.ecs", pars);
            });

            server.Get("/404", (req, res) =>
            {
                res.SendString("404");
            });

            server.Get("/*", (req, res) =>
            {
                res.Redirect("/404");
            });


            server.InitializeDefaultPlugins(false, new SimpleHttpSecuritySettings(2, 20000));
            //server.AddPlugin<SimpleSQLiteDatatase, SimpleSQLiteDatatase>(new SimpleSQLiteDatatase("./db.sqlite"));

            server.Start(true);
        }
    }
}
