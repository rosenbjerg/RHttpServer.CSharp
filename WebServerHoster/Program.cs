using System;
using RHttpServer;
using RHttpServer.Plugins.Default;
using RHttpServer.Plugins.External;

namespace RHSCommandLine
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new RHttpServer.RHttpServer(3000, 3, "./public");
            
            server.Get("/", (req, res) =>
            {
                var sessionId = req.Cookies["ss-id"];
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

            server.Get("/:test", (req, res) =>
            {
                var pars = server.CreateRenderParams();
                pars.Add("data1", req.Params["test"]);
                pars.Add("data2", 42);

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
            server.AddPlugin<SimpleSQLiteDatatase, SimpleSQLiteDatatase>(new SimpleSQLiteDatatase("./db.sqlite"));

            server.Start(true);
            Console.ReadKey();
        }
    }
}
