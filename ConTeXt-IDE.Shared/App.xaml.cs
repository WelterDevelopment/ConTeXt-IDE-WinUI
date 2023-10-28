using CodeEditorControl_WinUI;
using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.ViewManagement;

namespace ConTeXt_IDE
{
 public partial class App : Application

 {
	public static ViewModel VM { get; set; }
	public static MainWindow MainWindow { get; set; }

	//private const string MutexName = "##||ConTeXt_IDE||##";
	//private Mutex _mutex;
	//bool createdNew;

	

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

	private void AI_Activated(object sender, AppActivationArguments e)
	{
	 switch (e.Data)
	 {
		case Windows.ApplicationModel.Activation.LaunchActivatedEventArgs Args:
		 VM.LaunchArguments = Args.Arguments;
		 VM.CurrentProject = VM.Default.ProjectList.FirstOrDefault(x => x.Name == Args.Arguments);
		 break;
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

	protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
	{
	 AppInstance DefaultAI = AppInstance.FindOrRegisterForKey("MainAppInstance");
	 AppInstance CurrentAI = AppInstance.GetCurrent();
	 var activation = CurrentAI.GetActivatedEventArgs();

  StartUp();

	 if (DefaultAI != CurrentAI)
	 {
		if (!VM.Default.MultiInstance)
		{

		 var instances = AppInstance.GetInstances();
		 //	Debug.WriteLine(string.Join(", ", instances.Select(x => x.ProcessId)));

		 if (instances.Count > 1)
		 {
			await DefaultAI.RedirectActivationToAsync(activation);
			instances.Remove(CurrentAI); // does absolutely nothing
			Exit();
			Environment.Exit(0);
			return;
		 }
		 else
		 {
			CurrentAI.Activated += AI_Activated;
		 }
		}
	 }
	 else
	 {
		CurrentAI.Activated += AI_Activated;
	 }

	 

	 switch (activation.Data)
	 {
		case Windows.ApplicationModel.Activation.LaunchActivatedEventArgs Args: VM.LaunchArguments = Args.Arguments; break;
	 }

	 MainWindow = new MainWindow();
	 MainWindow.Activate();

	 // It seems we don't need the old Mutex stuff anymore.
	 //_mutex = new Mutex(true, MutexName, out createdNew);
	 //if (!createdNew && !VM.Default.MultiInstance)
	 //{
	 //	Application.Current.Exit();
	 //	Environment.Exit(0);

	 //	return;
	 //}
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