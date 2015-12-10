using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IISProgrammaticTest {
    class Program {

        public static Process IisProcess { get; set; }
        static void Main(string[] args) {

            ProcessStartInfo processStartInfo = new ProcessStartInfo() {
                ErrorDialog = false,
                CreateNoWindow = true,
                UseShellExecute = false,
                Arguments = string.Format("/path:\"{0}\" /port:{1}", @"C:\code\lab\IISProgrammaticTest\Webtest1", 574)
            };

            string path = (!string.IsNullOrEmpty(processStartInfo.EnvironmentVariables["programfiles(x86)"]) ? processStartInfo.EnvironmentVariables["programfiles(x86)"] : processStartInfo.EnvironmentVariables["programfiles"]) + "\\IIS Express\\iisexpress.exe";

            processStartInfo.FileName = path;

            IisProcess = new Process {
                StartInfo = processStartInfo
            };

            IisProcess.Start();
            Console.Read();
            Console.WriteLine("ready to stop");
            Shutdown();
        }

        public static void Shutdown() {
            if (IisProcess == null) {
                return;
            }

            IisProcess.Close();
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
