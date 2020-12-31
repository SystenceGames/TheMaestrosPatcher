using System;
using System.IO;

namespace SetFolderPermissions
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            //startInfo.UseShellExecute = false;
            //startInfo.RedirectStandardError = true;

            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C Icacls \"" + dir + "\" /T /C /grant Everyone:F";

            //log(startInfo.Arguments);

            process.StartInfo = startInfo;
            process.Start();
            //log(process.StandardError.ReadToEnd());
        }

        //static void log(string input)
        //{
        //    string strLogText = input;

        //    // Create a writer and open the file:
        //    StreamWriter log;

        //    if (!File.Exists("logfile.txt"))
        //    {
        //        log = new StreamWriter("logfile.txt");
        //    }
        //    else
        //    {
        //        log = File.AppendText("logfile.txt");
        //    }

        //    // Write to the file:
        //    log.WriteLine(DateTime.Now);
        //    log.WriteLine(strLogText);
        //    log.WriteLine();

        //    // Close the stream:
        //    log.Close();
        //}
    }
}
