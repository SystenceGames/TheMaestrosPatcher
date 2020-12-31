using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Threading;
using System.Windows;

namespace The_Maestros_Patcher
{
    /*
     * Hi there! The Imagine! Updater wants to supply you with a free and easy way to keep your customers software up to date.
     * This code is licensed under the Apache 2.0 license.
     * If you use this for any bigger project like an indie game or something feel free to shoot me a mail to fe.a.ernst@gmail.com so I have something to brag about but no pressure!
     * 
     * HowTo:
     * 1. Find the directory of the program you want to have distributed by this software, copy the path to the textBox next to the Create XML-Button and press it!
     * 2. Check the register.xml that was created on your desktop. It should show all the files and folder you want to have synced with your customers.
     *      Be sure none of your files use weird charakters because some are not supported by xml. I replaced ' ' with '+' so '+' is not supported either but ' ' is.
     * 3. Upload that directory and the register.xml to a public part of your server. The two free hosters I tested used to have a "public_html"-folder that served that purpose.
     * 4. Change the patchPath string below to match the path to the public part of your server. You may want to test different URL using the download function of this updater.
     *      For example try to enter an URL ending in "/register.xml" to find your freshly uploaded files.
     * 5. Start the program and hope everything works. You will need administrative rights if you patch to some directories.
     * 
     * 6. Feel free to change the other configurations and the code itself to your liking.
     */
    static class configurations
    {
        // the url to your server repository.
        public static string patchPath;

        private static string gameFolder;

        // I decided to put my files in a bin folder next to the updater to keep things together. But based on this it could become necessary to start the updater with administrative rights.
        public static string savePath
        {
            get { return Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory) + gameFolder; }
        }

        public static string ExecutableDomain;
        // The name of the executable at the root of your savePath. This will be executed when pressing the playButton.
        public static string ExecutableName;
        public static string fileForPatcherConfigs = "\\TMPatcherConfigs.xml";
        // How the Updater is labeled.
        public static string windowName = "The Maestros Patcher";
        // The main element in your register. It doesn't matter what you call it.
        public static string xmlRootName = "CoreOverload";
        // the files added to the notUpdatedFiles string[] won't be added to the xml and therefore not be downloaded and patched. Furthermore files of that kind won't be deleted when found in the savePath.
        // I needed this function for one annoying file that somehow changed it's sha1-Hashcode for being downloaded so it was always shown as outdated (not disturbing the functionality of the program).
        // You might want to dig into the code when you experience similar issius but need your file patched for new versions. But the one I was talking about was created on runtime anyway.
        public static List<string> notUpdatedFiles = new List<string>();

        public static void initConfigs()
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(exeDir + fileForPatcherConfigs); 

            XmlNodeList patchPathNode = xmlDoc.GetElementsByTagName("patchPath");
            XmlNodeList gameFolderNode = xmlDoc.GetElementsByTagName("gameFolder");
            XmlNodeList execDomainNode = xmlDoc.GetElementsByTagName("executableDomain");
            XmlNodeList executableNameNode = xmlDoc.GetElementsByTagName("executableName");
            try
            {
                patchPath = patchPathNode[0].InnerText;
                gameFolder = gameFolderNode[0].InnerText;
                ExecutableDomain = execDomainNode[0].InnerText;
                ExecutableName = executableNameNode[0].InnerText;
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                () =>
                {
                    ((MainWindow)App.Current.MainWindow).notify("Something was wrong with your TMPatcherConfigs.xml configuration file");
                }));
            }

            // TODO check the files to exclude node first
            XmlNodeList filesToExcludeNodes = xmlDoc.GetElementsByTagName("excludeFile");

            for (int i = 0; i < filesToExcludeNodes.Count; i++)
            {
                notUpdatedFiles.Add(filesToExcludeNodes.Item(i).InnerText);
            }
        }

        //http://patches.blob.core.windows.net/tm-release/
        public static void editPatchPathEnding(string input)
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(exeDir + fileForPatcherConfigs);
            XmlNodeList patchPathNode = xmlDoc.GetElementsByTagName("patchPath");
            var url = new Uri(patchPathNode[0].InnerText);
            string path = url.AbsolutePath;
            string pathLeft = path.Substring(0, path.IndexOf('-') + 1);
            string newURL = url.GetLeftPart(UriPartial.Authority) + pathLeft + input + '/';

            patchPathNode[0].InnerText = newURL;

            xmlDoc.Save(exeDir + fileForPatcherConfigs);
        }
        public static string getPatchPathEnding()
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(exeDir + "\\TMPatcherConfigs.xml");
            XmlNodeList patchPathNode = xmlDoc.GetElementsByTagName("patchPath");
            var url = new Uri(patchPathNode[0].InnerText);
            string path = url.AbsolutePath;
            path = path.Substring(path.IndexOf('-') + 1);
            return path.Substring(0, path.Length - 1);
        }
    }
}
