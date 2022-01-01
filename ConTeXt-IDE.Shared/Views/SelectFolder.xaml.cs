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
        [ComImport, System.Runtime.InteropServices.Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize([In] IntPtr hwnd);
        }

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetActiveWindow();

        ViewModel vm = App.VM;
        public SelectFolder()
        {
            App.VM.SelectedPath = "";
            this.InitializeComponent();
        }


        public StorageFolder folder;

        private async  void Button_Click(object sender, RoutedEventArgs e)
        {

            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");
            folderPicker.CommitButtonText = "Open";
            folderPicker.SettingsIdentifier = "ChooseWorkspace";
            folderPicker.ViewMode = PickerViewMode.List;
            IInitializeWithWindow initializeWithWindowWrapper = folderPicker.As<IInitializeWithWindow>();
            IntPtr hwnd = GetActiveWindow();
            initializeWithWindowWrapper.Initialize(hwnd);
            folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                App.VM.SelectedPath = folder.Path;
                this.PrimaryButtonText = "Select";
            }
        }
    }
}
