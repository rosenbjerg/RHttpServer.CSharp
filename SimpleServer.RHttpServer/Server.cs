using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RHttpProxy;
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
            //var proxy = new Forwarder(5000, 5, false);
            //proxy.Add("localhost", new ForwardingProxyHandler("localhost:5001"));

            var server = new HttpServer(5000, 4, "", true);
            server.CachePublicFiles = false;

            server.Get("/daw", (req, res) => { res.SendString("ok1"); });
            server.Get("/daw/*", (req, res) => { res.SendString("no1"); });

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

            server.Post("/newuserpost", (req, res) =>
            {
                var data = req.GetBodyPostFormData();
                res.SendString("ok");
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

            Logger.Configure(LoggingOption.File, true, "./log.txt");
            //server.InitializeDefaultPlugins(renderCaching: true, securityOn: false, securitySettings: new SimpleHttpSecuritySettings(2, 20000));
            
            server.InitializeDefaultPlugins(false);
            server.GetPlugin<IFileCacheManager>().CacheAllowedFileExtension.Add(".ico");
            server.Start(true);

            //proxy.Start(true);
            //var server2 = new HttpServer(5000, 2, "");
            //server2.Get("/", (req, res) => { res.SendString("ok2"); });
            //server2.Start("localhost");
        }
    }
}