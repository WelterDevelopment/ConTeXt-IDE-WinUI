using ConTeXt_IDE.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using WinRT;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConTeXt_IDE
{
	public sealed partial class SelectFolder : ContentDialog
	{

		ViewModel vm = App.VM;
		public SelectFolder()
		{
			App.VM.SelectedPath = "";
			this.InitializeComponent();
		}


		public StorageFolder folder;

		private async void Button_Click(object sender, RoutedEventArgs e)
		{

			var folderPicker = new FolderPicker();
			folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
			folderPicker.FileTypeFilter.Add("*");
			folderPicker.SettingsIdentifier = "ChooseWorkspace";
			folderPicker.ViewMode = PickerViewMode.List;

			// See the sample code below for how to make the window accessible from the App class.
			var window = App.MainWindow;

			// Retrieve the window handle (HWND) of the current WinUI 3 window.
			var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

			// Initialize the folder picker with the window handle (HWND).
			WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);


		
			folder ??= await folderPicker.PickSingleFolderAsync();
			if (folder != null)
			{
				App.VM.SelectedPath = folder.Path;
				this.PrimaryButtonText = "Select";
			}
		}
	}
}
