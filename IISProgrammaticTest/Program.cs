using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Cassandra;
using IISProgrammaticTest.Tools;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace IISProgrammaticTest {
    internal class Program
    {
        private static void Main(string[] args)
        {
            ExpressWebsiteStarter starter = new ExpressWebsiteStarter(44300, @"C:\test2\config");
            ProjectCache cache = new ProjectCache(@"C:\test2\projects");
            starter.StartWebsiteGroup(
                cache.BuildProject(
                    "https://connectionseducation.visualstudio.com/DefaultCollection/SoftwareDevelopment/V3%20Services%20Team%201/_git/UsersService"));
            starter.StartWebsiteGroup(
                cache.BuildProject(
                    "https://connectionseducation.visualstudio.com/DefaultCollection/SoftwareDevelopment/V3%20Services%20Team%201/_git/SitesService"));

            Console.WriteLine("site is up!");
            Console.Read();
            Console.WriteLine("ready to stop");
            starter.StopAll();
        }

        public class CcmCommandIssuer
        {
            public string ClusterName { get; }
            public string Version { get; }
            public string HostIp { get; }

            public CcmCommandIssuer(string clusterName, string version, string ipAddr)
            {
                ClusterName = clusterName;
                Version = version;
                HostIp = ipAddr;
            }

            public void CreateCluster(int nodeCount)
            {
                ExecuteCcm("remove");
                ExecuteCcm($"create {ClusterName} -v {Version}");
                ExecuteCcm($"populate -n {nodeCount}");
                ExecuteCcm("start");
            }

            public void ExecuteCcm(string args, int timeout = 1000)
            {
                var executable = "cmd.exe";
                args = "/c ccm " + args;

                ExecuteProcess(executable, args, timeout);
            }

            private int ExecuteProcess(string processName, string args, int timeout)
            {
                int exitCode;

                using (var process = new Process())
                {
                    process.StartInfo.FileName = processName;
                    process.StartInfo.Arguments = args;

                    process.Start();

                    if (process.WaitForExit(timeout))
                    {
                        // Process completed.
                        exitCode = process.ExitCode;
                    }
                    else
                    {
                        // Timed out.
                        exitCode = -1;
                    }
                }

                return exitCode;
            }
        }
    }

}
