using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DoubleDashPlotter
{
    /// <summary>
    /// Interaction logic for DependencyDownload.xaml
    /// </summary>
    public partial class DependencyDownload : Window
    {

        private string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vc_redist.x64.exe");

        InstallDepCheck installRedist = new InstallDepCheck();

        ProgressBar progressBar = new ProgressBar();

        Label label = new Label();

        bool debugDependencyOverride = false;

        bool isDolhpinInstaleld;


        public DependencyDownload()
        {
            InitializeComponent();

            CheckIfDependencyDivExists();

            bool isRedistInstalled = installRedist.IsVcRedistInstalled();

            if (!isRedistInstalled || !isDolhpinInstaleld)
            {
                ShowDownloadNotice();
            }
            else {
                PlotterPoint plotterPoint = new PlotterPoint(true);

                plotterPoint.Show();

                this.Close();
            }
            

            

        }

        private void ShowDownloadNotice()
        {
            // Construct the message
            string message = "This program will download and install the following components:\n\n" +
                "1. Visual C++ Redistributable: https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist\n" +
                "2. Dolphin Emulator: https://dolphin-emu.org/\n" +
                "Do you want to continue with the download?";

            // Display the message box
            MessageBoxResult result = MessageBox.Show(message, "Download Notice", MessageBoxButton.YesNo, MessageBoxImage.Information);

            // Handle the user's choice
            if (result == MessageBoxResult.Yes)
            {
                bool isRedistInstalled = installRedist.IsVcRedistInstalled();

                // Proceed with downloads
                DownloadVSRedist2020();
                DownloadDolphin();
            }
            else
            {
                // Exit the application
                this.Close();
                Application.Current.Shutdown();
            }

            async void DownloadVSRedist2020()
            {

                string fileUrlVSRedist = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
                string destinationFilePathVSRedist = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vs_17_redist.exe");

                Label_Download.Content = "Downloading Visual C++ 2015-2022 Redistributables...";

                Progress<int> progress = new Progress<int>(percent =>
                {
                    ProgressBar_Download.Value = percent;
                });

                Progress<int> progress_Setup = new Progress<int>(percent =>
                {
                    ProgressBar_Setup.Value = percent;
                });

                await DownloadFileAsync(fileUrlVSRedist, destinationFilePathVSRedist, progress);
                Label_Setup.Content = "Installing Visual C++ 2015-2022 Redistributales...";
                await RunInstallerAndWaitAsync(destinationFilePathVSRedist, progress_Setup);
            }

            async void DownloadDolphin()
            {
                string fileUrlDolhpin = "https://dl.dolphin-emu.org/releases/2409/dolphin-2409-x64.7z";
                string destinationFilePathDolphin = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Depends", "Dolphin");

                Label_Download.Content = "Downloading Dolphin Emulator...";

                Progress<int> progress = new Progress<int>(percent =>
                {
                    ProgressBar_Download.Value = percent;
                });

                Progress<int> progress_Setup = new Progress<int>(percent =>
                {
                    ProgressBar_Setup.Value = percent;
                });

                await DownloadFileAsync(fileUrlDolhpin, System.IO.Path.Combine(destinationFilePathDolphin, "dolphin-2409-x64.7z"), progress);
                Label_Setup.Content = "Extracting Dolphin Emulator...";
                await ArchiveExtractor.Extract7zAsync(System.IO.Path.Combine(destinationFilePathDolphin, "dolphin-2409-x64.7z"), destinationFilePathDolphin, progress_Setup);



            }
        }

        private async Task DownloadFileAsync(string url, string destinationFilePath, IProgress<int> progress)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;

                                if (canReportProgress)
                                {
                                    var percent = (int)((totalRead * 1d) / (totalBytes * 1d) * 100);
                                    progress.Report(percent);
                                }
                            }
                        }
                        while (isMoreToRead);
                    }
                }
            }
        }

        private async Task RunInstallerAndWaitAsync(string installerPath, IProgress<int> progress)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "", // Empty for normal installation (non-silent)
                UseShellExecute = true, // To open the installer window
            };

            Process installerProcess = new Process
            {
                StartInfo = startInfo
            };

            installerProcess.Start();

            // Simulate progress update while waiting for the installer to complete
            int progressValue = 0;

            // While the process is running, slowly increase progress
            while (!installerProcess.HasExited)
            {
                progressValue = Math.Min(progressValue + 10, 90); // Increase progress slowly, cap at 90%
                progress.Report(progressValue);
                await Task.Delay(1000); // Wait for 1 second between progress updates
            }

            // When the installer finishes, set progress to 100%
            progress.Report(100);
        }

        private void CheckIfDependencyDivExists()
        {
            // Get the path of the executable
            string exePath = AppDomain.CurrentDomain.BaseDirectory;

            // Construct the path to the Depends/Dolphin folder
            string dependsPath = System.IO.Path.Combine(exePath, "Depends");
            string dolphinPath = System.IO.Path.Combine(dependsPath, "Dolphin");
            string dolphinexecutable = System.IO.Path.Combine(dolphinPath, "Dolphin.exe");

            // Check if Depends folder exists, if not create it
            if (!Directory.Exists(dependsPath))
            {
                Directory.CreateDirectory(dependsPath);
                Console.WriteLine($"Created directory: {dependsPath}");
            }

            // Check if Dolphin folder exists inside Depends, if not create it
            if (!Directory.Exists(dolphinPath))
            {
                Directory.CreateDirectory(dolphinPath);
                Console.WriteLine($"Created directory: {dolphinPath}");
            }
            else
            {
                Console.WriteLine($"Directory {dolphinPath} already exists.");
            }

            if (!File.Exists(dolphinexecutable))
            {
                isDolhpinInstaleld = false;
            }
            else
            {
                isDolhpinInstaleld = true;
            }
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
