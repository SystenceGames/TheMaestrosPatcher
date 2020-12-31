using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Xml;
using System.Linq;
using System.Windows;
using System.IO;
using System.Collections.Concurrent;

namespace The_Maestros_Patcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string[] args = Environment.GetCommandLineArgs();

            configurations.initConfigs();

            //args[1] is the address to download from
            //E.G. "The Maestros Patcher.exe" http://patches.maestrosgame.com/tm-release/
            if (args.Length == 2)
            {
                //This is a hack, makes a lot of assumptions about the state of the rest of the code
                configurations.patchPath = args[1];
                try
                {
                    List<string[]> filesToDownload = Patching.prepareFilesListToPatch();
                    ConcurrentBag<String> filesToRedownload = Patching.downloadListofFilesUntillDone(new System.Net.WebClient(), filesToDownload);
                    if (filesToRedownload.Count > 0)
                    {
                        Exception err = new Exception("Failed to download " + filesToRedownload.Count + " file(s)");
                        int x = 0;
                        foreach (String file in filesToRedownload)
                        {
                            x++;
                            err.Data.Add(x.ToString(), file);
                        }
                        throw err;
                    }
                }
                catch (Exception exp)
                {
                    Console.Error.WriteLine(exp.ToString());
                    foreach (System.Collections.DictionaryEntry error in exp.Data)
                    {
                        //Console.Error.WriteLine(error);
                        Console.Error.WriteLine(error.Key + ": " + error.Value);
                    }
                    this.Shutdown(2);
                }
                this.Shutdown(0);
                 
            }

            //args[1] is the folder to scan
            //args[2] is the folder to save the xml scanned into
            //e.g. "The Maestros Patcher.exe" "C:\TheMaestros\Tools\The Maestros Patcher\The Maestros Patcher\bin\Debug" "C:\TheMaestros\Tools\The Maestros Patcher\The Maestros Patcher\bin\Debug"
            if (args.Length == 3)
            {
                /* do stuff without a GUI */
                try
                {
                    
                    XmlDocument createdXml = XMLCreation.createXmlDoc(args[1]);
                    if (File.Exists(args[2] + "\\register.xml"))
                    {
                        File.Delete(args[2] + "\\register.xml");
                    }
                    createdXml.Save(args[2] + "\\register.xml");
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp.ToString());
                    //arbitrarily defined error code
                    this.Shutdown(3);
                }
                this.Shutdown(0);
            }
            else
            {
                this.MainWindow = new MainWindow();
                this.MainWindow.Show();
            }
        }
    }
}
