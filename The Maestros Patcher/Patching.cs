using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace The_Maestros_Patcher
{
    static class Patching
    {
        private static ConcurrentBag<String> filesToRedownload = new ConcurrentBag<String>();
        private static Queue<string[]> filesToDownload;
        private static WebClient webClient;
        static ManualResetEvent resetEvent = new ManualResetEvent(false);
        public static List<string[]> prepareFilesListToPatch()
        {
            List<string[]> filesToDownload = new List<String[]>();

            // First, double check \\Game exists
            if (!Directory.Exists(configurations.savePath))
                Directory.CreateDirectory(configurations.savePath);

            XmlDocument savePathFiles = XMLCreation.createXmlDoc(configurations.savePath);

            XmlDocument onlinePatchFiles = new XmlDocument();
            onlinePatchFiles.Load(Patching.streamFile(configurations.patchPath + "register.xml"));

            queueFilesFromDirectory(ref filesToDownload, onlinePatchFiles.DocumentElement, savePathFiles.DocumentElement, configurations.patchPath, configurations.savePath);

            return filesToDownload;
        }

        private static void queueFilesFromDirectory(ref List<String[]> filesToDownload, XmlElement patchFileDirectory, XmlElement saveDirectory, string patchPath, string savePath)
        {
            XmlNodeList patchFiles = patchFileDirectory.SelectNodes("file");
            XmlNodeList patchFolders = patchFileDirectory.SelectNodes("directory");
            XmlNodeList savedFiles = null;
            XmlNodeList savedFolders = null;
            if (saveDirectory != null)
            {
                savedFiles = saveDirectory.SelectNodes("file");
                savedFolders = saveDirectory.SelectNodes("directory");
            }

            // handle the next deeper level
            if (patchFolders != null)
            {
                foreach (XmlNode patchFolder in patchFolders)
                {
                    XmlNode saveDirectoryOfPatchFolder = null;
                    if (savedFolders != null)
                    {
                        foreach (XmlNode savedFolder in savedFolders)
                        {
                            if (patchFolder.Attributes.Item(0).Value == savedFolder.Attributes.Item(0).Value)
                            {
                                saveDirectoryOfPatchFolder = savedFolder;
                            }
                        }
                    }
                    if (saveDirectoryOfPatchFolder == null)
                    {
                        Directory.CreateDirectory(savePath + "\\" + patchFolder.Attributes.Item(0).Value);
                    }
                    queueFilesFromDirectory(ref filesToDownload, (XmlElement)patchFolder, (XmlElement)saveDirectoryOfPatchFolder, patchPath + patchFolder.Attributes.Item(0).Value + "/", savePath + "\\" + patchFolder.Attributes.Item(0).Value);
                }
            }

            // download missing files
            foreach (XmlNode patchFile in patchFiles)
            {
                bool needsToBeDownloaded = true;
                if (savedFiles != null)
                {

                    foreach (XmlNode savedFile in savedFiles)
                    {
                        if (savedFile.Attributes.Item(0).Value == patchFile.Attributes.Item(0).Value
                            && savedFile.Attributes.Item(1).Value == patchFile.Attributes.Item(1).Value)
                        {
                            needsToBeDownloaded = false;
                        }
                    }
                }
                if (needsToBeDownloaded)
                {
                    filesToDownload.Add(new String[4] { patchPath, patchFile.Attributes.Item(0).Value.Replace("+", " "), savePath + "\\", patchFile.Attributes.Item(0).Value });
                }

            }
        }

        #region Download
        /// <summary>
        /// Returns a stream of the given url.
        /// </summary>
        /// <param name="fullPath">an URL of an element</param>
        /// <returns></returns>
        public static Stream streamFile(string fullPath)
        {
            Uri uri = new Uri(fullPath);

            // Get the object used to communicate with the server.
            WebClient webRequest = new WebClient();
            return webRequest.OpenRead(uri);
        }
        /// <summary>
        /// Downloads a file.
        /// </summary>
        /// <param name="path">the online directory ending in '/'</param>
        /// <param name="fileNameWithExtension">fileName.extension</param>
        /// <param name="savePath">the save path that can be copy pasted out of the windows explorer in a format like (C:\Program Files)</param>
        public static void downloadAllFiles()
        {
            if(filesToDownload.Count <= 0) 
            {
                resetEvent.Set();
                return;
            }

            string[] file = filesToDownload.Dequeue();
            string path = file[0];
            string fileNameWithExtension = file[1];
            string savePath = file[2];
            
            try
            {
                Uri uri = new Uri(path + fileNameWithExtension);
                webClient.DownloadFileAsync(uri, savePath + fileNameWithExtension, fileNameWithExtension);
            }
            catch (Exception exp)
            {
                filesToRedownload.Add( "Failed to download: " + fileNameWithExtension + "\n" + exp.Message);
                downloadAllFiles();
            }
        }

        private static void fileCompletedHandler(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                filesToRedownload.Add("Failed to download: " + (e.UserState as string) + "\n" + e.Error.Message + 
                    ((e.Error.InnerException != null) ? ("\n" + e.Error.InnerException.Message ): ""));
            }
            downloadAllFiles();
        }

        public static ConcurrentBag<String> downloadListofFilesUntillDone(WebClient client, List<string[]> listOfFiles)
        {
            webClient = client;
            filesToDownload = new Queue<string[]>(listOfFiles);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(fileCompletedHandler);

            if (listOfFiles.Count > 0)
            {
                Thread downloadStarter = new Thread(downloadAllFiles);
                downloadStarter.Start();
                resetEvent.WaitOne();
            }

            return filesToRedownload;
        }
        #endregion
    }
}
