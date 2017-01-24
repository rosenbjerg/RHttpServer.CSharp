using System;
using System.Net;
using RHttpServer;
using RHttpServer.Logging;
namespace SimpleRHttpServer
{
    internal class Server
    {
        private static void Main(string[] args)
        {
            var server = new TaskBasedHttpServer(5000, "public", true);
            server.CachePublicFiles = false;
            server.WebSocket("/ws/:url", (req, wsd) =>
            { 
                var url = req.Params["url"];
                wsd.OnTextReceived += (sender, eventArgs) =>
                {
                    Console.WriteLine(eventArgs.Text);
                    wsd.SendText("OK, i got the message: '" + eventArgs.Text + "' from " +url);
                };
                wsd.OnClosed += (sender, eventArgs) =>
                {
                    Console.WriteLine("Closed");
                };
                wsd.Ready();
            });

            server.Get("/", (req, res) =>
            {
                res.SendString("ok1");
            });
            server.Get("/*", (req, res) =>
            {
                res.SendString("ello");
            });

            server.Get("/url2", (req, res) =>
            {
                res.SendString("url2");
            });

            server.Get("/url2/*", (req, res) =>
            {
                res.SendString("url2fallback1");
            });
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

            server.Post("/upload", async (req, res) =>
            {
                int c = 0;
                var done = await req.SaveBodyToFile("up");
                Console.WriteLine(c);
                Console.WriteLine(done);
                res.SendString("OK");
            });
            
            server.Post("/newuserpost", (req, res) =>
            {
                var data = req.GetBodyPostFormData();
                res.SendString("ok");
            });
            
            Logger.Configure(LoggingOption.File, true, "./log.txt");
            //server.InitializeDefaultPlugins(renderCaching: true, securityOn: false, securitySettings: new SimpleHttpSecuritySettings(2, 20000));

            server.InitializeDefaultPlugins(true);
            server.GetPlugin<IFileCacheManager>().CacheAllowedFileExtension.Add(".ico");
            server.Start(true);
        }
    }
}