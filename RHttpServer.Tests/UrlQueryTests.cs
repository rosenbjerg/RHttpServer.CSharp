using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RHttpServer.Tests
{
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