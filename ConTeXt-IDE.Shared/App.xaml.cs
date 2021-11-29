using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI.Xaml;
using System;
using System.Threading;
using Windows.Storage;
using Windows.UI.ViewManagement;

namespace ConTeXt_IDE
{
    public partial class App : Application

    {
        public static ViewModel VM { get; set; }
        public static MainWindow mainWindow;

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
                RequestedTheme = Settings.Default.Theme == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }
            catch (Exception ex)
            {
                write(ex.Message);
            }
        }

        public static async void write(string stringtowrite)
        {
            StorageFolder sf = ApplicationData.Current.LocalFolder;
            var file = await sf.CreateFileAsync("Error-Log.txt", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, stringtowrite);
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            write("Unhandled app exception:" + e.Message + " - " + e.Exception.StackTrace);
            VM?.Log("Unhandled app exception:" + e.Message);
        }
        
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            StartUp();
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew && !VM.Default.MultiInstance)
            {
                Application.Current.Exit();
                return;
            }
            mainWindow = new MainWindow();
            mainWindow.Activate();
        }

        private void StartUp()
        {
            try
            {
                Resources.TryGetValue("VM", out object Vm);
                if (Vm != null)
                {
                    VM = Vm as ViewModel;
                }
                else VM = new ViewModel();

                var setting = ((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]);
                setting.Theme = VM.Default.Theme == "Light" ? ElementTheme.Light : ElementTheme.Dark;
                setting.AccentColor = VM.AccentColor.Color;
                //ColorPaletteResources palette = new ColorPaletteResources();
                //palette.Accent = setting.AccentColorLow;
                //App.Current.Resources.MergedDictionaries.Add(palette);

            }
            catch (Exception ex)
            {
                VM.Log("Exception on Startup: " + ex.Message);
            }
        }
    }
}
