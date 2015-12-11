using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISProgrammaticTest.Tools
{
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
                File.Copy("./Utilities/nuget.txt", destPath);
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

