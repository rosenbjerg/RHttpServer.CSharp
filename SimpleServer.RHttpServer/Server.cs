using RHttpServer;
using RHttpServer.Logging;
using RHttpServer.Plugins;
using RHttpServer.Plugins.Default;

namespace SimpleRHttpServer
{
    internal class Server
    {
        private static void Main(string[] args)
        {
            var server = new HttpServer(3000, 3, "./public");

            server.Get("/", (req, res) => { res.SendString("ok"); });

            server.Get("/file", (req, res) => { res.SendFile("./public/index.html"); });

            server.Get("/render", (req, res) =>
            {
                var pars = server.CreateRenderParams();
                pars.Add("data1", "test1");
                pars.Add("data2", "{\"Test\":\"test2\"}");
                res.RenderPage("./public/index.ecs", pars);
            });

            server.Get("/download", (req, res) =>
            {
                res.Download("./ServiceStack.Text.dll");
            });

            server.Get("/render2", (req, res) =>
            {
                var pars = server.CreateRenderParams();
                pars.Add("data1", "test2");
                pars.Add("data2", "{\"Test\":\"test2\"}");
                res.RenderPage("./public/index.ecs", pars);
            });

            server.Get("/render/3", (req, res) =>
            {
                var pars = server.CreateRenderParams();
                pars.Add("data1", "test1");
                pars.Add("data2", "{\"Test\":\"test2\"}");
                res.RenderPage("./public/index.ecs", pars);
            });

            server.Get("/:par1/:par2", (req, res) =>
            {
                var pars = server.CreateRenderParams();
                pars.Add("data1", req.Params["par1"]);

                var q = req.Params["par2"];
                if (!q.EndsWith("?")) q += "?";
                pars.Add("data2", "{\"question\":\"" + q + "\", \"answer\":" + 42 + "}");

                res.RenderPage("./public/index.ecs", pars);
            });

            server.Get("/404", (req, res) => { res.SendString("404"); });

            server.Get("/*", (req, res) => { res.Redirect("/404"); });

            Logger.Configure(LoggingOptions.File, true, "./log.txt");

            server.InitializeDefaultPlugins(renderCaching: false, securityOn: false, securitySettings: new SimpleHttpSecuritySettings(2, 20000));

            server.Start(true);
        }
    }
}