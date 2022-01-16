using CodeEditorControl_WinUI;
using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Threading;
using Windows.ApplicationModel.Activation;
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
				var uiSettings = new UISettings();
				var defaultthemecolor = uiSettings.GetColorValue(UIColorType.Background);

				

				if (Settings.Default.Theme == "Light")
				{
					RequestedTheme = ApplicationTheme.Light;
				}
				else if (Settings.Default.Theme == "Dark")
				{
					RequestedTheme = ApplicationTheme.Dark;
				}
				else
				{
					//RequestedTheme = defaultthemecolor == Colors.White ? ApplicationTheme.Light : ApplicationTheme.Dark;
				}
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

		protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{
			StartUp(); 

			VM.LaunchArguments = args.Arguments;

			_mutex = new Mutex(true, MutexName, out createdNew);
			if (!createdNew && !VM.Default.MultiInstance)
			{
				Application.Current.Exit();
				Environment.Exit(0);

				return;
			}

			//var aargs = AppInstance.GetCurrent().GetActivatedEventArgs();

			
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
				var accentColor = VM.AccentColor;

				if (accentColor != null)
				{
					setting.Theme = (ElementTheme)Enum.Parse(typeof(ElementTheme), Settings.Default.Theme);
					setting.AccentColor = accentColor.Color;
					Application.Current.Resources["SystemAccentColor"] = accentColor.Color;
					Application.Current.Resources["SystemAccentColorLight2"] = setting.AccentColorLow;
					Application.Current.Resources["SystemAccentColorDark1"] = setting.AccentColorLow.ChangeColorBrightness(0.1f);
					Application.Current.Resources["WindowCaptionBackground"] = setting.AccentColorLow;
					Application.Current.Resources["WindowCaptionBackgroundDisabled"] = setting.AccentColorLow;
				}

			}
			catch (Exception ex)
			{
				VM.Log("Exception on Startup: " + ex.Message);
			}
		}
	}
}
