using System;
using System.Linq;
using RHttpServer;
using RHttpServer.Logging;
using RHttpServer.Plugins;
using RHttpServer.Plugins.Default;
using RHttpServer.Response;

namespace SimpleRHttpServer
{
    internal class Server
    {
        private static void Main(string[] args)
        {
            var server = new HttpServer(5000, 3, "./public", true) {CachePublicFiles = false};

            //server.Get("/", (req, res) => { res.SendString("ok"); });

            //server.Get("/file", (req, res) => { res.SendFile("./public/index.html"); });

            server.Get("/render", (req, res) =>
            {
                var pars = new RenderParams
                {
                    {"data1", "test1"},
                    { "data2", "{\"Test\":\"test2\"}"}
                };
                res.RenderPage("./public/index.ecs", pars);
            });

            //server.Get("/render2", (req, res) =>
            //{
            //    var pars = server.CreateRenderParams();
            //    pars.Add("data1", "test2");
            //    pars.Add("data2", "{\"Test\":\"test2\"}");
            //    res.RenderPage("./public/index.ecs", pars);
            //});

            //server.Get("/render/3", (req, res) =>
            //{
            //    var pars = server.CreateRenderParams();
            //    pars.Add("data1", "test1");
            //    pars.Add("data2", "{\"Test\":\"test2\"}");
            //    res.RenderPage("./public/index.ecs", pars);
            //});

            server.Post("/postdata", (req, res) =>
            {
                var v = new {S = "", Daw = 39};
                req.SaveBodyToFile("./public/down", s => "temptemp" + s);
                //var reas = req.GetBodyPostFormData();
                //var fname = reas["fname"];
                //var lname = reas["lname"];
                res.SendString($"Hi");
            });

            //server.Get("/:par1/:par2", (req, res) =>
            //{
            //    var pars = server.CreateRenderParams();
            //    pars.Add("data1", req.Params["par1"]);

            //    var q = req.Params["par2"];
            //    if (!q.EndsWith("?")) q += "?";
            //    pars.Add("data2", "{\"question\":\"" + q + "\", \"answer\":" + 42 + "}");

            //    res.RenderPage("./public/index.ecs", pars);
            //});

            //server.Get("/404", (req, res) => { res.SendString("404"); });

            //server.Get("/*", (req, res) => { res.Redirect("/404"); });

            //Logger.Configure(LoggingOption.File, true, "./log.txt");

            //server.InitializeDefaultPlugins(renderCaching: true, securityOn: false, securitySettings: new SimpleHttpSecuritySettings(2, 20000));
            server.HttpsEnabled = true;

            server.Start(true);
        }
    }
}