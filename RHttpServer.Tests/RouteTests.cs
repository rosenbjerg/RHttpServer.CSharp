using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RHttpServer.Tests
{
    [TestClass]
    public class RouteTests
    {
        [ClassInitialize]
        public static void RouteTestsInitialize(TestContext testContext)
        {
            _testServ = new TaskBasedHttpServer(5555, "");

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

            _testServ.Get("/param/:param", (req, res) =>
            {
                res.SendString(req.Params["param"]);
            });
            
            _testServ.Start(true);
        }
        
        [ClassCleanup]
        public static void MyClassCleanup()
        {
            _testServ.Stop();
        }

        private static TaskBasedHttpServer _testServ;
        
        [TestMethod]
        public void TestDirectRoutes()
        {
            using (var wc = new WebClient())
            {
                var str = wc.DownloadString("http://localhost:5555/url1");
                Assert.AreEqual("url1", str);

                str = wc.DownloadString("http://localhost:5555/url2");
                Assert.AreEqual("url2", str);

                str = wc.DownloadString("http://localhost:5555/url2/url3");
                Assert.AreEqual("url2url3", str);
            }
        }

        [TestMethod]
        public void TestFallback()
        {
            using (var wc = new WebClient())
            {
                var str = wc.DownloadString("http://localhost:5555/url2/faw");
                Assert.AreEqual("url2fallback1", str);

                str = wc.DownloadString("http://localhost:5555/url2/dgfsdkglj");
                Assert.AreEqual("url2fallback1", str);

                str = wc.DownloadString("http://localhost:5555/urdgfsdkglj");
                Assert.AreEqual("fallback21", str);
            }
        }

        [TestMethod]
        public void TestParams()
        {
            using (var wc = new WebClient())
            {
                var str = wc.DownloadString("http://localhost:5555/param/test");
                Assert.AreEqual("test", str);

                str = wc.DownloadString("http://localhost:5555/param/test2");
                Assert.AreEqual("test2", str);
            }
        }
    }
    [TestClass]
    public class UrlQueryTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            _testServ = new TaskBasedHttpServer(5556, "");

            _testServ.Get("/url1", (req, res) =>
            {
                var q = req.Queries["q"];
                res.SendString(q);
            });
            

            _testServ.Get("/url2", (req, res) =>
            {
                var x = req.Queries["x"];
                var y = req.Queries["y"];
                res.SendString(x + "-" +y);
            });
            
            _testServ.Start(true);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _testServ.Stop();
        }

        private static TaskBasedHttpServer _testServ;

        [TestMethod]
        public void TestSingleQuery()
        {
            using (var wc = new WebClient())
            {
                var str = wc.DownloadString("http://localhost:5556/url1?q=url1");
                Assert.AreEqual("url1", str);
            }
        }

        [TestMethod]
        public void TestTwoQueries()
        {
            using (var wc = new WebClient())
            {
                var str = wc.DownloadString("http://localhost:5556/url2?x=1&y=2");
                Assert.AreEqual("1-2", str);
            }
        }
    }
}
