using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Net;
using System.Windows.Threading;
using System.Threading;
using KonamiCode;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace The_Maestros_Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string patchPath { get { return configurations.patchPath; } }
        string savePath { get { return configurations.savePath; } }
        string COLauncherPath { get { return configurations.ExecutableDomain; } }
        string ExecutableName { get { return configurations.ExecutableName; } }// executable to launch after you finish downloading & click "Launch"
        public static string xmlRootName { get { return configurations.xmlRootName; } }
        public static System.Threading.Thread autoPatchThread;

        private KonamiSequence sequence = new KonamiSequence();
        public static List<String[]> filesToDownload;
        public static int numberOfFilesToDownload;
        public static int alreadyProcessedFiles;
        string[] notifications;
        string patchnotes = string.Empty;
        public static Stopwatch sw = new Stopwatch();
        private static int swReadings = 0;
        private static long lastBytes = 0;
        private static int maxDataPoints = 20;
        private static int measurements = 0;
        private static double[] speedDataPoints = new double[maxDataPoints];

        public delegate void updateContentsText(string notification);
        public delegate void patchButtonDel(bool boolean);
        public delegate void playButtonDel(bool boolean);
        public delegate void patchlognotesButtonDel(bool boolean);
        public delegate void patchlognotesButtonSwitchDel();
        public delegate void updateProgressBarDel();
        public delegate void fileProgressBarDel();

        public bool bCanPlay = false;
        public bool bDownloading = false;

        public MainWindow()
        {
            InitializeComponent();


            notifications = new string[3];

            //filesToDownload = new List<String[]>();
            // Start checking if we need to patch
            autoPatchThread = new System.Threading.Thread(CheckPatchState);

            // necessary or else the process doesn't die when the window closes
            // otherwise you keep downloading even when the window isn't open
            autoPatchThread.IsBackground = true;

            this.DisableDownloadPlayButton();

            autoPatchThread.Start();

        }

        #region OriginalFunctionality

        #region GUI

        public void errorNotify(string notification)
        {
            notify(notification);

            this.NotificationTB.Foreground = Brushes.Red;
        }

        /// <summary>
        /// Add a message as the first line in the contentsTextBox.
        /// </summary>
        /// <param name="notification">the added message</param>
        public void notify(string notification)
        {
            if (!this.InfoBox.Dispatcher.CheckAccess())
            {
                updateContentsText d = new updateContentsText(notify);
                InfoBox.Dispatcher.Invoke(d, new object[] { notification });
            }
            else
            {
                // find the latest notification, put it above the bar
                NotificationTB.Text = notification;
                this.NotificationTB.Foreground = Brushes.Indigo;
            }
        }

        public void updateProgressBar()
        {
            if (!this.DownloadProgressBar.Dispatcher.CheckAccess())
            {
                updateProgressBarDel d = new updateProgressBarDel(updateProgressBar);
                this.DownloadProgressBar.Dispatcher.Invoke(d, new object[] { });
            }
            else
            {
                try
                {
                    this.DownloadProgressBar.Value = ((float)(alreadyProcessedFiles) / (float)numberOfFilesToDownload) * 100;
                    //this.DownloadProgressBar.Value = (1 - Math.Pow(1 - ((float)(alreadyProcessedFiles) / (float)numberOfFilesToDownload), 1 / 3.0)) * 100;
                }
                catch (Exception e)
                {
                    this.DownloadProgressBar.Value = 0;
                }
            }
        }

        public void updateFileProgressBar()
        {
            //if (!this.FileProgressBar.Dispatcher.CheckAccess())
            //{
            //    fileProgressBarDel d = new fileProgressBarDel(updateFileProgressBar);
            //    this.FileProgressBar.Dispatcher.Invoke(d, new object[] { });
            //}
            //else
            //{
            //    try
            //    {
            //        this.FileProgressBar.Value = ((float)(alreadyProcessedFiles) / (float)numberOfFilesToDownload) * 100;
            //        //this.DownloadProgressBar.Value = (1 - Math.Pow(1 - ((float)(alreadyProcessedFiles) / (float)numberOfFilesToDownload), 1 / 3.0)) * 100;
            //    }
            //    catch (Exception e)
            //    {
            //        this.FileProgressBar.Value = 0;
            //    }
            //}
        }

        #endregion

        /// <summary>
        /// This is called when this Patcher is started.
        /// It will contact the server, compare the online and offline files, show possibly available patchnotes and give appropriate notifications.
        /// </summary>
        private void CheckPatchState()
        {
            //System.Threading.Thread.Sleep(100);

            try
            {
                notify("Connecting to patch-server...");

                // tries to grab patch notes
                try
                {
                    StreamReader sr = new StreamReader(Patching.streamFile(patchPath + "patchnotes.txt"), System.Text.Encoding.Default, true);
                    patchnotes = sr.ReadToEnd();
                    if (!patchnotes.StartsWith("<html>"))
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                        () =>
                        {
                            InfoBox.AppendText(patchnotes);
                        }));
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                        () =>
                        {
                            InfoBox.AppendText("Could not retrieve Patch Notes");
                            ServerStatusTB.Text = "Server Status: Offline";
                            ServerStatusTB.Foreground = Brushes.Red;
                        }));
                    }
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                    () =>
                    {
                        errorNotify("Cannot reach patch-server.  Please try again another time.");
                        ServerStatusTB.Text = "Server Status: Offline";
                        ServerStatusTB.Foreground = Brushes.Red;

                        //ShowDownloadButton(); // maybe?
                        //DisableDownloadPlayButton();
                    }));
                    return;
                }

                notify("Checking local files...");

                alreadyProcessedFiles = 0;
                filesToDownload = Patching.prepareFilesListToPatch();
                numberOfFilesToDownload = filesToDownload.Count;

                if (filesToDownload.Count > 0)
                {
                    gameNeedsToUpdate();
                }
                else
                {
                    gameIsUpToDate();
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                () =>
                {
                    errorNotify("There was an error checking the status of your local files");
                }));
            }
        }
        public void gameNeedsToUpdate()
        {
            bCanPlay = false;
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                () =>
                {
                    notify("Your version is outdated. Press Download to update.");
                    ShowDownloadButton();
                }));
        }
        public void gameIsUpToDate()
        {
            // double check that the executable is there.
            if (File.Exists(savePath + "\\" + COLauncherPath + ExecutableName))
            {
                bCanPlay = true;
                bDownloading = false;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                () =>
                {
                    notify("Your version is up to date!");
                    alreadyProcessedFiles = numberOfFilesToDownload = 1;
                    updateProgressBar();
                    ShowPlayButton();
                }));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                () =>
                {
                    notify("Cannot find " + ExecutableName);
                }));
            }
        }

        private static void fileProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                         () =>
                         {
                             ((MainWindow)App.Current.MainWindow).FileProgressBar.Value = e.ProgressPercentage;
                            // swReadings++;
                            // double seconds = sw.Elapsed.TotalSeconds;
                            // if (seconds >= 0.05)
                            // {
                            //     //some taken from http://stackoverflow.com/questions/19212852/webclient-downloaddataasync-current-download-speed
                            //     sw.Restart();
                            //     var dataReceived = e.BytesReceived - lastBytes;
                            //     if (dataReceived > 0)
                            //     {
                            //         double dataPoint = dataReceived / seconds;
                            //         speedDataPoints[measurements++ % maxDataPoints] = dataPoint;
                            //     }
                            //     lastBytes = e.BytesReceived;
                            //
                            // }
                            // if (swReadings >= 10)
                            // {
                            //     swReadings = 0;
                            //     ((MainWindow)App.Current.MainWindow).SpeedTB.Text = string.Format("{0} KB/s ", (speedDataPoints.Average() / 1024).ToString("0"));
                            // }
                             ((MainWindow)App.Current.MainWindow).FileTB.Text = string.Format("{0} MB's / {1} MB's of {2}",
                                (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                                (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"),
                                e.UserState as string);

                         }));
        }

        private static void fileCompletedHandler(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Interlocked.Increment(ref MainWindow.alreadyProcessedFiles);
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                         () =>
                         {
                             ((MainWindow)App.Current.MainWindow).updateProgressBar();
                         }));
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                    () =>
                    {
                        ((MainWindow)App.Current.MainWindow).notify("Downloaded " + MainWindow.alreadyProcessedFiles + "/" + MainWindow.numberOfFilesToDownload + " files");
                    }));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                            () =>
                            {
                                ((MainWindow)App.Current.MainWindow).errorNotify("Failed to download file: " + (e.UserState as string));
                            }));
            }
        }
        /// <summary>
        /// It will update the progressBar and give appropriate notifications.
        /// </summary>
        public static void patch()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
            () =>
            {
                ((MainWindow)App.Current.MainWindow).notify("Downloaded " + MainWindow.alreadyProcessedFiles + "/" + MainWindow.numberOfFilesToDownload + " files");
            }));
            sw.Start();

            using (WebClient client = new WebClient())
            {
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(fileCompletedHandler);
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(fileProgressChanged);

                ConcurrentBag<String> filesToRedownload = Patching.downloadListofFilesUntillDone(client, MainWindow.filesToDownload);

                if (filesToRedownload.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                           () =>
                           {
                               string baseMessage = "Failed to download " + filesToRedownload.Count + " files, please check your internet connection and try again\n";
                               ((MainWindow)App.Current.MainWindow).errorNotify(baseMessage);
                               string result;
                               if (filesToRedownload.TryPeek(out result))
                               {
                                   MessageBox.Show(baseMessage + result);
                               }
                               else
                               {
                                   MessageBox.Show(baseMessage);
                               }

                               System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                               Application.Current.Shutdown();
                           }));
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
                    () =>
                    {
                        ((MainWindow)App.Current.MainWindow).SpeedTB.Text = "";
                        ((MainWindow)App.Current.MainWindow).FileTB.Text = "";
                        ((MainWindow)App.Current.MainWindow).notify("Update completed.");
                        ((MainWindow)App.Current.MainWindow).bCanPlay = true;
                        ((MainWindow)App.Current.MainWindow).bDownloading = false;
                        ((MainWindow)App.Current.MainWindow).ShowPlayButton();
                    }));
                }
            }

        }

        #region start program

        public void startProgram()
        {
            try
            {
                ProcessStartInfo launcherPSI = new ProcessStartInfo(savePath + "\\" + COLauncherPath + ExecutableName);
                launcherPSI.WorkingDirectory = savePath + "\\" + COLauncherPath;
                Process.Start(launcherPSI);
                System.Environment.Exit(0);
            }
            catch { }
        }
        #endregion

        #endregion OriginalFunctionality

        #region ButtonToggles

        /* WARNING, USES A PSEUDO ENABLE, NOT THE IsEnabled variable! */
        public void ShowDownloadButton()
        {
            this.DownloadOrPlayBtn.Click -= this.DownloadOrPlayClicked;
            this.DownloadOrPlayBtn.Click += this.DownloadOrPlayClicked;

            BitmapImage bitimg = (BitmapImage)FindResource("downloadbutton");

            // Set Button.Background
            this.DownloadOrPlayBtn.Background = new ImageBrush(bitimg);
            this.DownloadOrPlayBtn.IsEnabled = true;
        }


        /* WARNING, USES A PSEUDO ENABLE, NOT THE IsEnabled variable! */
        public void ShowPlayButton()
        {
            this.DownloadOrPlayBtn.Click -= this.DownloadOrPlayClicked;
            this.DownloadOrPlayBtn.Click += this.DownloadOrPlayClicked;

            BitmapImage bitimg = (BitmapImage)FindResource("playbutton");

            // Set Button.Background
            this.DownloadOrPlayBtn.Background = new ImageBrush(bitimg);
            this.DownloadOrPlayBtn.IsEnabled = true;
        }

        /* WARNING, USES A PSEUDO DISABLE, NOT THE IsEnabled variable! */
        public void DisableDownloadPlayButton()
        {
            this.DownloadOrPlayBtn.Click -= this.DownloadOrPlayClicked;

            BitmapImage bitimg = (BitmapImage)FindResource("downloadbutton_gray");

            // Set Button.Background
            this.DownloadOrPlayBtn.Background = new ImageBrush(bitimg);
        }

        #endregion ButtonToggles

        #region UICallbacks

        private void DownloadOrPlayClicked(object sender, RoutedEventArgs e)
        {
            if (bCanPlay)
            {
                this.startProgram();
            }
            else if (!bDownloading)
            {
                bCanPlay = false;
                bDownloading = true;
                DisableDownloadPlayButton();
                autoPatchThread = new System.Threading.Thread(patch);
                autoPatchThread.Start();
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (sequence.IsCompletedBy(e.Key))
            {
                easterEgg();
            }
        }

        public void easterEgg()
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Which Patch Lines do you want", "Change patch Line", configurations.getPatchPathEnding(), -1, -1);
            configurations.editPatchPathEnding(input);
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        #endregion UICallbacks

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /* kind of a hack, not positive what the reprecussions are,
             * but it seems to be doing the job */
            autoPatchThread.Abort();
        }
    }

}


namespace KonamiCode
{

    public class KonamiSequence
    {

        List<Key> Keys = new List<Key>{Key.Up, Key.Up, 
                                       Key.Down, Key.Down, 
                                       Key.Left, Key.Right, 
                                       Key.Left, Key.Right, 
                                       Key.B, Key.A};
        private int mPosition = -1;

        public int Position
        {
            get { return mPosition; }
            private set { mPosition = value; }
        }

        public bool IsCompletedBy(Key key)
        {

            if (Keys[Position + 1] == key)
            {
                // move to next
                Position++;
            }
            else if (Position == 1 && key == Key.Up)
            {
                // stay where we are
            }
            else if (Keys[0] == key)
            {
                // restart at 1st
                Position = 0;
            }
            else
            {
                // no match in sequence
                Position = -1;
            }

            if (Position == Keys.Count - 1)
            {
                Position = -1;
                return true;
            }

            return false;
        }

    }
}