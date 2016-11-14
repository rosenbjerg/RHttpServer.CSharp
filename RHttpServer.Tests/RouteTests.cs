using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RHttpServer.Tests
{
    [TestClass]
    public class RouteTests
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            _testServ = new HttpServer(5555, 3, "");

            _testServ.Get("/url1", (req, res) =>
            {
                res.SendString("url1");
            });

            _testServ.Get("/*", (req, res) =>
            {
                res.SendString("fallback21");
            });

            _testServ.Get("/url2", (req, res) =>
            {
                res.SendString("url2");
            });

            _testServ.Get("/url2/*", (req, res) =>
            {
                res.SendString("url2fallback1");
            });

            _testServ.Get("/url2/url3", (req, res) =>
            {
                res.SendString("url2url3");
            });

            _testServ.Get("/:param", (req, res) =>
            {
                res.SendString(req.Params["param"]);
            });
            
            _testServ.Start(true);
        }

        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup]
        public static void MyClassCleanup()
        {
            _testServ.Stop();
        }

        private static HttpServer _testServ;
        
        
        [TestMethod]
        public void TestRoutes()
        {
            using (var wc = new WebClient())
            {
                var str = wc.DownloadString("http://localhost:5555/url1");
                Assert.AreEqual("url1", str);
                
                str = wc.DownloadString("http://localhost:5555/url2");
                Assert.AreEqual("url2", str);

                str = wc.DownloadString("http://localhost:5555/url2/faw");
                Assert.AreEqual("url2fallback1", str);

                str = wc.DownloadString("http://localhost:5555/url2/dgfsdkglj");
                Assert.AreEqual("url2fallback1", str);

                str = wc.DownloadString("http://localhost:5555/url2/url3");
                Assert.AreEqual("url2url3", str);

                str = wc.DownloadString("http://localhost:5555/test");
                Assert.AreEqual("test", str);

                str = wc.DownloadString("http://localhost:5555/test2");
                Assert.AreEqual("test2", str);
            }
        }

    }
}
