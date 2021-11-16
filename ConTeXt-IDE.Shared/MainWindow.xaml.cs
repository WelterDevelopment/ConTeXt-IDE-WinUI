using ConTeXt_IDE.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ConTeXt_IDE
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
      
        private ViewModel VM { get; } = App.VM;
        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(CustomDragRegion);
            Title = "ConTeXt IDE";
        }

        public static bool CheckForInternetConnection(int timeoutMs = 5000, string url = "https://www.google.com/")
        {
            try
            {
              
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using var response = (HttpWebResponse)request.GetResponse();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void InstallEvergreen()
        {

            VM.InfoMessage(true, "Installing...", "Downloading the Evergreen WebView2 Runtime.", InfoBarSeverity.Informational);

            if (!File.Exists(Path.Combine(ApplicationData.Current.LocalFolder.Path, "evergreen.exe")))
            {
                if (CheckForInternetConnection())
                {
                    if(!await DownloadEvergreen())
                    {
                        VM.InfoMessage(true, "Error", "Something went wrong. Please try again later.", InfoBarSeverity.Error);
                    }
                }
                else
                {
                    await Task.Delay(1000);
                    VM.InfoMessage(true, "No Internet", "Please enable your internet connection and restart this app! This app needs the Evergreen WebView2 Runtime to get installed.", InfoBarSeverity.Error);
                }
             }
            else
            {
                Install();
            }
        }

        private async Task<bool> DownloadEvergreen()
        {
            try
            {
                WebClient wc = new WebClient();

                wc.DownloadProgressChanged += (a, b) => { VM.ProgressValue = b.ProgressPercentage; };
                wc.DownloadFileCompleted += (a, b) =>
                {
                    Install();
                };
                wc.DownloadFileAsync(new System.Uri("https://go.microsoft.com/fwlink/p/?LinkId=2124703"), Path.Combine(ApplicationData.Current.LocalFolder.Path, "evergreen.exe"));
                return true;
            }
            catch
            {
                return false;
            }


        }

        private async void Install()
        {
            VM.IsIndeterminate = true;
            VM.InfoMessage(true, "Installing...", "Please wait up to 2 minutes for the Evergreen WebView2 Runtime to install.", InfoBarSeverity.Informational);
            bool InstallSuccessful = false;
            await Task.Run(async () => { InstallSuccessful = await InstallTask(); });
            if (InstallSuccessful)
            {
                VM.Default.EvergreenInstalled = true;
                VM.InfoMessage(true, "Success!", "The editor and viewer controls are now fully operational.", InfoBarSeverity.Success);
                VM.IsVisible = false;
                await Task.Delay(2500);
                VM.IsIndeterminate = true;
                RootFrame.Navigate(typeof(MainPage));
            }
            else
            {
                VM.InfoMessage(true, "Error", "Something went wrong. Please try again later.", InfoBarSeverity.Error);
                VM.IsVisible = false;
            }
        }


        private async Task<bool> InstallTask()
        {
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo(Path.Combine(ApplicationData.Current.LocalFolder.Path, "evergreen.exe"))
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = false,
                 
                Verb = "runas",
            };
            p.OutputDataReceived += (e, f) =>
            { //VM.Log(f.Data.);
            };
            //p.ErrorDataReceived += (e, f) => {Log(f.Data); };
            p.StartInfo = info;
            
          
            p.Start();
            p.WaitForExit();

            return p.ExitCode == 0;
        }
        private async void RootFrame_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!VM.Default.EvergreenInstalled)
                {
                    string RegKey = System.Environment.Is64BitOperatingSystem ? @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" : @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
                    var version = Registry.GetValue(RegKey, "pv", null);
                    VM.Default.EvergreenInstalled = version != null && !string.IsNullOrWhiteSpace(version?.ToString());
                }
            }
            catch { }

            try
            {
                if (VM.Default.EvergreenInstalled)
                {
                    VM.IsIndeterminate = true;
                    RootFrame.Navigate(typeof(MainPage));
                }
                else
                {
                    await Task.Delay(1000);
                    InstallEvergreen();
                }
            }
            catch { }
        }

    }
}
