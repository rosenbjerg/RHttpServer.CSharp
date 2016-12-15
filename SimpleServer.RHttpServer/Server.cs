using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
            var server = new HttpServer(5000, 4, "public", true);
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
            server.Post("/newuserpost", (req, res) =>
            {
                var data = req.GetBodyPostFormData();
                res.SendString("ok");
            });
            
            Logger.Configure(LoggingOption.File, true, "./log.txt");
            //server.InitializeDefaultPlugins(renderCaching: true, securityOn: false, securitySettings: new SimpleHttpSecuritySettings(2, 20000));
            
            server.InitializeDefaultPlugins(false);
            server.GetPlugin<IFileCacheManager>().CacheAllowedFileExtension.Add(".ico");
            server.Start(true);
            
        }
    }
}