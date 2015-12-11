using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace IISProgrammaticTest {
    class Program {
        static void Main(string[] args)
        {
            List<string> buildFolder = new List<string>();
            ExpressWebsiteStarter starter = new ExpressWebsiteStarter(44300, @"C:\test2\config");
            ProjectCache cache = new ProjectCache(@"C:\test2\projects");
            starter.StartWebsiteGroup(cache.BuildProject("https://connectionseducation.visualstudio.com/DefaultCollection/SoftwareDevelopment/V3%20Services%20Team%201/_git/UsersService"));
            starter.StartWebsiteGroup(cache.BuildProject("https://connectionseducation.visualstudio.com/DefaultCollection/SoftwareDevelopment/V3%20Services%20Team%201/_git/SitesService"));          
            


//            var configFilePath = @"C:\test2\IISProgrammaticTest\IISProgrammaticTest\test.config";
//            ProcessStartInfo processStartInfo = new ProcessStartInfo() {
//                ErrorDialog = false,
//                CreateNoWindow = true,
//                UseShellExecute = false,
////                Arguments = string.Format("/path:\"{0}\" /port:{1} /config:", @"C:\code\lab\IISProgrammaticTest\Webtest2", 574),
//                Arguments = $"/config:\"{configFilePath}\""
//            };

//            string path = (!string.IsNullOrEmpty(processStartInfo.EnvironmentVariables["programfiles(x86)"]) ? processStartInfo.EnvironmentVariables["programfiles(x86)"] : processStartInfo.EnvironmentVariables["programfiles"]) + "\\IIS Express\\iisexpress.exe";

//            processStartInfo.FileName = path;

//            IisProcess = new Process {
//                StartInfo = processStartInfo
//            };


//            IisProcess.Start();
            Console.WriteLine("site is up!");
            Console.Read();
            Console.WriteLine("ready to stop");
            starter.StopAll();
        }

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
                        x => new DirectoryInfo(x).GetDirectories().First(y => y.FullName.EndsWith("API")).FullName);

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

        public class ProjectCache
        {
            private readonly string _repoDir;
            private readonly Dictionary<string, string> _repoCache;

            public ProjectCache(string repoDir)
            {
                Directory.CreateDirectory(repoDir);
                _repoDir = repoDir;
                _repoCache = new Dictionary<string, string>();
            }

            public string BuildProject(string repositoryUrl)
            {
                string repoKey = repositoryUrl.ToLowerInvariant();

                if (!_repoCache.ContainsKey(repoKey))
                {
                    var path = CloneProject(repositoryUrl);
                    if (path != null)
                    {
                        NugetRestore(path);
                        Build("release", path);
                        _repoCache.Add(repoKey, path);
                    }
                }

                return _repoCache.ContainsKey(repoKey) ? _repoCache[repositoryUrl.ToLower()] : null;
            }

            private void Build(string configuration, string projectPath)
            {
                string slnPath = new DirectoryInfo(projectPath).GetFiles()
                    .FirstOrDefault(x => x.FullName.EndsWith("sln", StringComparison.InvariantCultureIgnoreCase))?
                    .FullName;

                StartProcess("msbuild.exe", $"{slnPath} /t:Build /p:Configuration={configuration} /p:TargetFramework=v4.5.2", projectPath);
            }

            private string CloneProject(string repositoryUrl)
            {
                int pos = repositoryUrl.LastIndexOf("/") + 1;
                string match = repositoryUrl.Substring(pos, repositoryUrl.Length - pos).ToLower();
                StartProcess("git.exe", $"clone \"{repositoryUrl}\"", _repoDir);
                return new DirectoryInfo(_repoDir).GetDirectories().First(x => x.Name.ToLower() == match)?.FullName;
            }

            private void NugetRestore(string projPath)
            {
                string destPath = Path.Combine(projPath, "nuget.exe");
                if (!File.Exists(destPath))
                {
                    File.Copy("./Utilities/nuget.exe", destPath);
                }

                StartProcess("nuget.exe", "restore", projPath);
            }

            private void StartProcess(string filename, string arguments, string workingDir)
            {
                ProcessStartInfo procInfo = new ProcessStartInfo();
                procInfo.CreateNoWindow = true;
                procInfo.RedirectStandardOutput = false;
                procInfo.UseShellExecute = false;
                procInfo.FileName = filename;
                
                Process gitProcess = new Process();
                procInfo.Arguments = arguments;
                procInfo.WorkingDirectory = workingDir;

                gitProcess.StartInfo = procInfo;
                gitProcess.Start();
                gitProcess.WaitForExit();
                gitProcess.Close();
            }
        }

        //        IISVersionManagerLibrary.IISVersionManager mgr = new IISVersionManagerLibrary.IISVersionManager();
        //        IISVersionManagerLibrary.IIISVersion ver = mgr.GetVersionObject("7.5", IISVersionManagerLibrary.IIS_PRODUCT_TYPE.IIS_PRODUCT_EXPRESS);
        //
        //        object obj1 = ver.GetPropertyValue("expressProcessHelper");
        //
        //        IISVersionManagerLibrary.IIISExpressProcessUtility util = obj1 as IISVersionManagerLibrary.IIISExpressProcessUtility;
        //        Console.WriteLine(util.ToString());
    }
}
