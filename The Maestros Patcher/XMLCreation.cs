using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace The_Maestros_Patcher
{

    static class XMLCreation
    {
        private static readonly Object writeLock = new Object();
        private static readonly Object readLock = new Object();
        /// <summary>
        /// Creates an XML of the given path and all its subdirectories and files with unique identifiers.
        /// </summary>
        /// <param name="path">the directory to register in an XML</param>
        /// <returns>the XML of the diretory</returns>
        public static XmlDocument createXmlDoc(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root;
            root = doc.CreateElement(configurations.xmlRootName);

            addDirectoryToNode(path, root, doc);

            doc.AppendChild(root);
            return doc;
        }
        private static void addDirectoryToNode(string path, XmlNode node, XmlDocument doc)
        {
            // randomly breaks, might want to keep it breaking
            if (path == string.Empty)
            {
                return;
            }


            string[] files = Directory.GetFiles(path);
            Parallel.ForEach(files, file =>
            {
                addFileNodeToNode(path, file, node, doc);
            });

            string[] directories = Directory.GetDirectories(path);
            foreach (string directory in directories)
            {
                string dirName = directory.Replace(" ", "+").Substring(path.Length + 1);
                bool add = true;
                for (int i = 0; i < configurations.notUpdatedFiles.Count; i++)
                {
                    if (dirName.Equals(configurations.notUpdatedFiles[i]))
                    {
                        add = false;
                    }
                }
                if (add)
                {
                    XmlNode directoryNode = doc.CreateElement("directory");
                    addDirectoryToNode(directory, directoryNode, doc);

                    XmlAttribute nameAttr = doc.CreateAttribute("name");
                    nameAttr.InnerXml = dirName;
                    directoryNode.Attributes.Append(nameAttr);
                    node.AppendChild(directoryNode);
                }
            }
        }
        private static void addFileNodeToNode(string path, string filePath, XmlNode node, XmlDocument doc)
        {
            string name = filePath.Substring(path.Length + 1);
            bool add = true;
            for (int i = 0; i < configurations.notUpdatedFiles.Count; i++)
                if (name.Equals(configurations.notUpdatedFiles[i]))
                    add = false;
            if (add)
            {
                XmlNode fileNode;     
                XmlAttribute nameAttr;
                XmlAttribute hashAttr;
                lock (readLock)
                {
                    fileNode = doc.CreateElement("file");
                    nameAttr = doc.CreateAttribute("name");
                    hashAttr = doc.CreateAttribute("hash");
                }

                nameAttr.InnerXml = filePath.Replace(" ", "+").Substring(path.Length + 1); //TODO BUG!? this max cause the path to cut off the last character if you pass in a trailing /
                fileNode.Attributes.Append(nameAttr);

                // add sha1-Hashcode as attribute
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);   // file to stream
                BufferedStream bufferedStream = new BufferedStream(fileStream);                     // stream to bytestream
                SHA1Managed sha1 = new SHA1Managed();
                byte[] hash = sha1.ComputeHash(bufferedStream);                                     // bytestream to hash
                string hashString = BitConverter.ToString(hash);
                fileStream.Close();

                hashAttr.InnerXml = hashString;
                fileNode.Attributes.Append(hashAttr);
                lock (writeLock)
                {
                    node.AppendChild(fileNode);
                }
            }
        }
        /// <summary>
        /// Used to calculate the patch progress.
        /// </summary>
        /// <param name="e">the main element of an XMLDocument received with XMLDocument.documentElement.</param>
        /// <returns>the number of files in the document, folders are not counted</returns>
        public static int countFilesInXml(XmlElement e)
        {
            int counter = e.ChildNodes.Count;
            XmlNodeList patchFolders = e.SelectNodes("directory");
            foreach (XmlNode patchFolder in patchFolders)
            {
                --counter;
                counter += countFilesInXml((XmlElement)patchFolder);
            }
            return counter;
        }
    }
}