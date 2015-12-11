using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using IISProgrammaticTest.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace IISProgrammaticTest
{
    [TestClass]
    public class ApiTest
    {
        private static ExpressWebsiteStarter _starter;

        [ClassInitialize]
        public static void Intialize(TestContext contxt)
        {
            _starter = new ExpressWebsiteStarter(44300, @"C:\test2\config");
            ProjectCache cache = new ProjectCache(@"C:\test2\projects");
            _starter.StartWebsiteGroup(
                cache.BuildProject(
                    "https://github.com/whonconnections/cassieApi"));
            _starter.StartWebsiteGroup(
                cache.BuildProject(
                    "https://github.com/whonconnections/atsApi"));
        }

        [TestMethod]
        public void Test()
        {
            using (HttpClient testClient = new HttpClient())
            {
                string userName = Guid.NewGuid().ToString();
                string postBody = JsonConvert.SerializeObject(new {Username = userName });
                testClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var test = testClient.PostAsync("https://localhost:44300/api/cas",
                    new StringContent(postBody, Encoding.UTF8, "application/json")).Result;

                var result = testClient.GetAsync("https://localhost:44301/api/ats").Result;
                var testResult = result.Content.ReadAsStringAsync().Result;
                Assert.IsTrue(!string.IsNullOrEmpty(testResult));
            }
        }

        [ClassCleanup]
        public static void TearDown()
        {
            _starter.StopAll();
        }
    }
}
