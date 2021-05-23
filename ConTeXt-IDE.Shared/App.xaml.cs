using ConTeXt_IDE.ViewModels;
using Microsoft.UI.Xaml;
using System;
using System.Threading;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ConTeXt_IDE
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application

    {
        public static ViewModel VM { get; set; }
        private MainWindow m_window;

        private const string MutexName = "##||ConTeXt_IDE||##";
        private Mutex _mutex;
        bool createdNew;

        public static MainPage MainPage;

        public App()
        {
            try
            {
                this.InitializeComponent();
                UnhandledException += App_UnhandledException;

                
            }
            catch (Exception ex)
            {
                write(ex.Message);
            }
        }

        async void write(string stringtowrite)
        {
            StorageFolder sf = ApplicationData.Current.LocalFolder;
            var file = await sf.CreateFileAsync("Error-Log.txt", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, stringtowrite);
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            VM?.Log("Unhandled app exception:" +  e.Message);
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            StartUp();
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew && !VM.Default.MultiInstance)
            {
                Application.Current.Exit();
                return;
            }
            m_window = new MainWindow();
            m_window.Activate();

        }

        private  void StartUp()
        {
            try
            {
                Resources.TryGetValue("VM", out object Vm);
                if (Vm != null)
                {
                    VM = Vm as ViewModel;
                }
                else VM = new ViewModel();

                //StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                RequestedTheme = VM.Default.Theme == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }
            catch (Exception ex)
            {
                VM.Log(ex.Message);
            }

        }
    }

  
}
