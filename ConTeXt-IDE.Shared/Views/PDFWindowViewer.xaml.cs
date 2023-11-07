using ConTeXt_IDE.Models;
using ConTeXt_IDE.Shared;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.UI;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace ConTeXt_IDE
{
	/// <summary>
	/// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
	/// </summary>
	public sealed partial class PDFWindowViewer : SystemBackdropWindow
	{
		public ViewModel VM { get; set; } = App.VM;

		public AppWindow AW { get; set; }

		public IntPtr hWnd;
		public bool IsCustomizationSupported { get; set; } = false;
		public PDFWindowViewer()
		{
			this.InitializeComponent();
			IsCustomizationSupported = AppWindowTitleBar.IsCustomizationSupported();
			AW = GetAppWindowForCurrentWindow();
			
			AW.Title = "PDF Viewer";
			AW.Closing += AW_Closing;
			string IconPath = Path.Combine(Package.Current.Installed­Location.Path, @"Assets", @"SquareLogo.ico");
			AW.SetIcon(IconPath);

			if (AW.Presenter is OverlappedPresenter OP)
			{
				if (VM.Default.WindowSettingPDF.IsMaximized)
				{
					OP.Maximize();
				}
				else
				{
					int x = Math.Max(VM.Default.WindowSettingPDF.LastSize.X, 0);
					int y = Math.Max(VM.Default.WindowSettingPDF.LastSize.Y, 0);
					AW.Move(new(x, y));
					AW.Resize(new(Math.Max(VM.Default.WindowSettingPDF.LastSize.Width, 200), Math.Max(VM.Default.WindowSettingPDF.LastSize.Height, 200)));
				}
			}

			if (IsCustomizationSupported)
			{
				ResetTitleBar();
			}
			else
			{
				CustomDragRegion.BackgroundTransition = null;
				CustomDragRegion.Background = null;
				ExtendsContentIntoTitleBar = true;
				CustomDragRegion.Height = 28;
				SetTitleBar(CustomDragRegion);
				Title = "PDF Viewer";
			}
		}


		public void ResetTitleBar()
		{
			AW.TitleBar.ExtendsContentIntoTitleBar = true;
			CustomDragRegion.Height = 32;
			AW.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
			AW.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AW.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(50, 125, 125, 125);
			AW.TitleBar.ButtonHoverForegroundColor = this.RequestedTheme == ElementTheme.Light ? Colors.Black : Colors.White;
			AW.TitleBar.ButtonForegroundColor = this.RequestedTheme == ElementTheme.Light ? Colors.Black : Colors.White;
			AW.TitleBar.ButtonInactiveForegroundColor = this.RequestedTheme == ElementTheme.Light ? Color.FromArgb(255, 50, 50, 50) : Color.FromArgb(255, 200, 200, 200);
			AW.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
		}

		private AppWindow GetAppWindowForCurrentWindow()
		{
			hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			Microsoft.UI.WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
			return AppWindow.GetFromWindowId(myWndId);
		}

		private async void AW_Closing(AppWindow sender, AppWindowClosingEventArgs args)
		{
			int x = Math.Max(sender.Position.X, 0);
			int y = Math.Max(sender.Position.Y, 0);
			int w = Math.Max(sender.Size.Width, 200);
			int h = Math.Max(sender.Size.Height, 200);
			bool ismax = false;
			if (sender.Presenter is OverlappedPresenter OP)
			{
			 ismax= OP.State == OverlappedPresenterState.Maximized;
			}
			VM.Default.WindowSettingPDF = new() { IsMaximized = ismax, LastPresenter = sender.Presenter.Kind, LastSize = new(x, y, w, h) };

			sender.Destroy();

		}

	}
}
