using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IISProgrammaticTest.Tools
{
    public class ExpressWebsiteStarter
    {
        private static string _configTemplate;
        private Dictionary<int, Process> _cache;
        private int _currentAvailablePort;
        private string _configPath;
        private readonly string _id;

        static ExpressWebsiteStarter()
        {
            _configTemplate = File.ReadAllText("./utilities/test.config");
        }

        public ExpressWebsiteStarter(int startPort, string configPath)
        {
            Directory.CreateDirectory(configPath);
            _cache = new Dictionary<int, Process>();
            _currentAvailablePort = startPort;
            _id = Guid.NewGuid().ToString();
            _configPath = configPath;
        }

        public int StartWebsiteGroup(params string[] projectPaths)
        {
            IEnumerable<string> starterProjectPaths =
                projectPaths.Select(
                    x => new DirectoryInfo(x).GetDirectories().First(y => y.FullName.ToLower().EndsWith("api")).FullName);

            int port = _currentAvailablePort;
            string configFilePath = Path.Combine(_configPath, $"{_id}_{port}.config");
            GenerateXmlDocument(configFilePath, port, starterProjectPaths.Select((value, index) => new { value, index })
                  .ToDictionary(pair => pair.index.ToString(), pair => pair.value));

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.CreateNoWindow = true;
            procInfo.RedirectStandardOutput = false;
            procInfo.UseShellExecute = false;
            procInfo.FileName = (!string.IsNullOrEmpty(procInfo.EnvironmentVariables["programfiles(x86)"]) ? procInfo.EnvironmentVariables["programfiles(x86)"] : procInfo.EnvironmentVariables["programfiles"]) + "\\IIS Express\\iisexpress.exe";

            Process proc = new Process();
            procInfo.Arguments = $"/config:\"{configFilePath}\"";

            proc.StartInfo = procInfo;
            proc.Start();


            _cache.Add(port, proc);
            _currentAvailablePort++;
            return port;
        }

        public void StopAll()
        {
            foreach (int key in _cache.Keys.ToArray())
            {
                StopWebsiteGroup(key);
            }
        }

        public void StopWebsiteGroup(int port)
        {
            if (_cache.ContainsKey(port))
            {
                _cache[port].Kill();
                _cache.Remove(port);
            }
        }

        private void GenerateXmlDocument(string finalName, int port, Dictionary<string, string> virtuals)
        {
            XDocument doc = XDocument.Parse(_configTemplate);
            var sitesNode = doc.Descendants("sites").FirstOrDefault();
            if (sitesNode != null)
            {
                XElement bindingsEle = new XElement("bindings", new XElement("binding", new XAttribute("protocol", "https"),
                    new XAttribute("bindingInformation", $"*:{port}:localhost")));
                XElement siteEle = new XElement("site", new XAttribute("name", $"web_{port}"), new XAttribute("id", "1"), new XAttribute("serverAutoStart", "true"));

                foreach (var virtualKey in virtuals.Keys)
                {
                    XElement appEle = new XElement("application", new XAttribute("path", "/"),
                        new XElement("virtualDirectory", new XAttribute("path", "/"), new XAttribute("physicalPath", virtuals[virtualKey])));

                    siteEle.Add(appEle);
                }

                siteEle.Add(bindingsEle);
                sitesNode.AddFirst(siteEle);
            }

            doc.Save(finalName);
        }
    }
}
