using CodeEditorControl_WinUI;
using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using ConTeXt_IDE.Shared.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;

namespace ConTeXt_IDE
{
    public sealed partial class MainPage : Page
    {
        private ViewModel VM { get; } = App.VM;

       

        public MainPage()
        {
            try
            {
                InitializeComponent();
                App.MainPage = this;
                // Listen to the Settings --> Update the fiddly docking manager
                //App.m_window.SetTitleBar(TabStripFooter);
                VM.Default.PropertyChanged += Default_PropertyChanged;

                //SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += MainPage_CloseRequested;
                
                TB_Version.Text = string.Format("Version: {0}.{1}.{2}.{3}",
                         Package.Current.Id.Version.Major,
                         Package.Current.Id.Version.Minor,
                         Package.Current.Id.Version.Build,
                         Package.Current.Id.Version.Revision);

                foreach (var FileEvent in VM.FileActivatedEvents)
                {
                    if (FileEvent != null)
                    {
                        foreach (StorageFile file in FileEvent.Files)
                        {
                            var fileitem = new FileItem(file) { };
                            VM.OpenFile(fileitem);
                        }
                    }
                }
                VM.FileActivatedEvents.Clear();

                MenuSave.Command = new RelayCommand(() => { Btnsave_Click(null, null); });
                MenuCompile.Command = new RelayCommand(() => { Btncompile_Click(null, null); });
                MenuSave.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control });
                MenuCompile.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Enter, Modifiers = VirtualKeyModifiers.Control });
                Binding myBinding = new Binding();
                myBinding.Path = new("CurrentFileItem.IsTexFile");
                MenuCompile.SetBinding(MenuFlyoutItem.IsEnabledProperty, myBinding);
            }
            catch (Exception ex)
            {
                App.VM?.InfoMessage(true, "Exception", ex.Message, InfoBarSeverity.Error);
            }
        }

        #region Page Load

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            

            SetColor(null, false);
        }

        public async void FirstStart()
        {
            if (App.VM.Default.FirstStart)
            {
                ShowTeachingTip("AddProject", Btnaddproject);
                App.VM.Default.FirstStart = false;
            }
        }

        private bool loaded = false;
       
        private void Edit_Unloaded(object sender, RoutedEventArgs e)
        {
            loaded = false;
        }

        private async void DisclaimerView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var p = Path.Combine(Package.Current.Installed­Location.Path, @"ConTeXt-IDE.Desktop", "web", @"About.html");
                StorageFile storageFile;

                if (File.Exists(p))
                {
                    storageFile = await StorageFile.GetFileFromPathAsync(p);
                }
                else
                {
                    storageFile = await StorageFile.GetFileFromApplicationUriAsync(new("ms-appx:///web/About.html"));
                }

                (sender as WebView2).Source = new System.Uri(storageFile.Path);
            }
            catch (Exception ex)
            {
                VM.Log(ex.Message);
            }
        }
       
        private MenuFlyoutItem MenuSave = new MenuFlyoutItem() { Text = "Save", Icon = new SymbolIcon() { Symbol = Symbol.Save } };
        private MenuFlyoutItem MenuCompile = new MenuFlyoutItem() { Text = "Compile", Icon = new SymbolIcon() { Symbol = Symbol.Play } };

        private async void Codewriter_Loaded(object sender, RoutedEventArgs e)
        {
            CodeWriter cw = sender as CodeWriter;

            VM.Codewriter = cw;

            if (!cw.ContextMenu.Items.Any(x => { if (x is MenuFlyoutItem item) return item.Text == "Save"; else return false; }))
                cw.Action_Add(MenuSave);

            if (!cw.ContextMenu.Items.Any(x => { if (x is MenuFlyoutItem item) return item.Text == "Compile"; else return false; }))
                cw.Action_Add(MenuCompile);

            if (VM.CurrentFileItem.IsTexFile)
                cw.UpdateSuggestions();

            cw.ScrollToLine(VM.CurrentFileItem.CurrentLine.iLine);
            cw.Focus(FocusState.Keyboard);

        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string file = @"tex\texmf-mswin\bin\context.exe";
            var storageFolder = ApplicationData.Current.LocalFolder;
            string filePath = System.IO.Path.Combine(storageFolder.Path, file);
            if (!VM.Default.DistributionInstalled && !File.Exists(filePath))
            {
                bool InstallSuccessful = false;
                VM.IsSaving = true;
                VM.IsPaused = false;
                VM.InfoMessage(true, "Installing", "Extracting the ConTeXt Distribution to the app's package Folder", InfoBarSeverity.Informational);
                await Task.Run(async () => { InstallSuccessful = await InstallContext(); });
                //InstallSuccessful = await InstallContext();
                if (InstallSuccessful)
                {
                    VM.Default.DistributionInstalled = true;
                    VM.IsSaving = false;
                    VM.IsPaused = true;
                    VM.IsVisible = false;
                    VM.InfoMessage(true, "Installation complete!", "Your ConTeXt distribution is up n' running!", InfoBarSeverity.Success);
                }
                else
                {
                    VM.InfoMessage(true, "ConTeXt Installation failed", "Please try again after reinstalling the app. Feel free to contact the app's author in case the problem persists!", InfoBarSeverity.Error);
                }
            }
            else if (File.Exists(filePath))
            {
                VM.Default.DistributionInstalled = true;
            }
            InitializeCommandReference();
            await VM.Startup();
            FirstStart();
            // OnProtocolActivated(Windows.ApplicationModel.AppInstance.GetActivatedEventArgs());

        }

        public void OnProtocolActivated(IActivatedEventArgs args)
        {
            if (args != null)
            {
                switch (args.Kind)
                {
                    case ActivationKind.File:
                        LoadFiles((FileActivatedEventArgs)args);
                        break;
                }
            }
        }

        private void LoadFiles(FileActivatedEventArgs args)
        {
            try
            {
                if (args != null)
                {
                    foreach (StorageFile sf in args.Files)
                    {
                        var fileitem = new FileItem(sf) { };
                        VM.OpenFile(fileitem);
                    }
                }
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async void PDFReader_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var p = Path.Combine(Package.Current.Installed­Location.Path, @"ConTeXt-IDE.Desktop", "web", @"viewer.html");
                StorageFile storageFile;


                if (File.Exists(p))
                {
                    storageFile = await StorageFile.GetFileFromPathAsync(p);
                }
                else
                {
                    storageFile = await StorageFile.GetFileFromApplicationUriAsync(new("ms-appx:///web/viewer.html"));
                }

                (sender as WebView2).Source = new System.Uri(storageFile.Path);
            }
            catch (Exception ex)
            {
                VM.Log(ex.Message);
            }
        }

        private void PDFReader_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            (sender as WebView2).CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            (sender as WebView2).CoreWebView2.Settings.AreDevToolsEnabled = false;
            (sender as WebView2).CoreWebView2.Settings.IsZoomControlEnabled = false;
        }

        #endregion

        #region Page Close

        // // This comes back in Project Reuinion 1.0
        //private async void MainPage_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        //{
        //    var deferral = e.GetDeferral();
        //    bool unsaved = App.VM.FileItems.Any(x => x.IsChanged);
        //    if (unsaved)
        //    {
        //        var save = new ContentDialog() { XamlRoot = XamlRoot, Title = "Do you want to save the open unsaved files before closing?", PrimaryButtonText = "Yes", SecondaryButtonText = "No", DefaultButton = ContentDialogButton.Primary };
        //        if (await save.ShowAsync() == ContentDialogResult.Primary)
        //        {
        //            await App.VM.SaveAll();
        //        }
        //    }
        //    deferral.Complete();
        //}

        #endregion

        #region File Operations

        public static List<FileItem> DraggedItems { get; set; } = new List<FileItem>();

        public static ObservableCollection<FileItem> DraggedItemsSource { get; set; }


        public static async Task CopyFolderAsync(StorageFolder sourceFolder, StorageFolder destinationFolder, string desiredName = null)
        {
            foreach (var folder in await sourceFolder.GetFoldersAsync())
            {
                var innerfolder = await destinationFolder.CreateFolderAsync(folder.Name, CreationCollisionOption.OpenIfExists);
                await CopyFolderAsync(folder, innerfolder);
            }
            foreach (var file in await sourceFolder.GetFilesAsync())
            {
                await file.CopyAsync(destinationFolder, file.Name, NameCollisionOption.ReplaceExisting);
            }
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            NewFile((sender as FrameworkElement).DataContext as FileItem);
        }
        private async void NewFile(FileItem fileItem)
        {
            try
            {
                string name = "newfile.tex";
                var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Set file name", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
                TextBox tb = new TextBox() { Text = name };
                cd.Content = tb;

                var res = await cd.ShowAsync();
                if (res == ContentDialogResult.Primary)
                {
                    name = tb.Text;
                    var folder = fileItem.File as StorageFolder;
                    if (await folder.TryGetItemAsync(name) == null)
                    {
                        var subfile = await folder.CreateFileAsync(name);
                        var fi = new FileItem(subfile) { Type = FileItem.ExplorerItemType.File };
                        fileItem.Children.Add(fi);
                    }
                    else
                        VM.InfoMessage(true, "Error", "The file" + name + " does already exist.", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            NewFolder((sender as FrameworkElement).DataContext as FileItem);
        }

        private void ColorsFlyout_Opening(object sender, object e)
        {
            VM.SystemAccentColor = (new Windows.UI.ViewManagement.UISettings()).GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
        }


        private async void NewFolder(FileItem fileItem)
        {
            try
            {
                string name = "newfolder";
                var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Set folder name", PrimaryButtonText = "ok", CloseButtonText = "cancel", DefaultButton = ContentDialogButton.Primary };
                TextBox tb = new TextBox() { Text = name };
                cd.Content = tb;

                var res = await cd.ShowAsync();
                if (res == ContentDialogResult.Primary)
                {
                    name = tb.Text;
                    var folder = fileItem.File as StorageFolder;
                    if (await folder.TryGetItemAsync(name) == null)
                    {
                        var subfolder = await folder.CreateFolderAsync(name);
                        var fi = new FileItem(subfolder) { Type = FileItem.ExplorerItemType.Folder };
                        fileItem.Children.Insert(0, fi);
                    }
                    else
                    {
                        VM.InfoMessage(true, "Error", "The folder" + name + " does already exist.", InfoBarSeverity.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async void AddFileRoot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = "file.tex";
                var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Set file name", PrimaryButtonText = "ok", CloseButtonText = "cancel", DefaultButton = ContentDialogButton.Primary };

                TextBox tb = new TextBox() { Text = name };
                cd.Content = tb;

                if (await cd.ShowAsync() == ContentDialogResult.Primary)
                {
                    var folder = VM.CurrentProject.Folder;
                    if (await folder.TryGetItemAsync(tb.Text) == null)
                    {
                        var file = await folder.CreateFileAsync(tb.Text);
                        var fi = new FileItem(file) { Type = FileItem.ExplorerItemType.File, FileLanguage = FileItem.GetFileType(file.FileType) };
                        VM.CurrentProject.Directory[0].Children.Add(fi);
                    }
                    else
                        VM.Log(name + " does already exist.");
                }
            }
            catch (Exception ex)
            {
                VM.Log("Exception on adding File: " + ex.Message);
            }
        }

        private async void AddFolderRoot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = "";
                var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Set folder name", PrimaryButtonText = "ok", CloseButtonText = "cancel", DefaultButton = ContentDialogButton.Primary };
                TextBox tb = new TextBox() { Text = name };
                cd.Content = tb;
                var result = await cd.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    name = tb.Text;
                    var folder = App.VM.CurrentProject.Folder;
                    if (await folder.TryGetItemAsync(name) == null)
                    {
                        var subfolder = await folder.CreateFolderAsync(name);
                        var fi = new FileItem(subfolder) { Type = FileItem.ExplorerItemType.Folder };
                        App.VM.CurrentProject.Directory[0].Children.Insert(0, fi);
                    }
                    else
                        App.VM.Log(name + " does already exist.");
                }
            }
            catch (Exception ex)
            {
                App.VM.Log("Exception on adding Folder: " + ex.Message);
            }
        }

        private async void OpeninExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchFolderAsync(App.VM.CurrentProject.Folder);
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fi = (FileItem)(sender as FrameworkElement).DataContext;
                RemoveItem(App.VM.CurrentProject.Directory, fi);
                VM.FileItems.Remove(fi);
                string type = fi.File is StorageFile ? "File" : "Folder";
                await fi.File.DeleteAsync();
                VM.Log(type + " " + fi.FileName + " removed.");
            }
            catch (Exception ex)
            {
                VM.Log(ex.Source + " : " + ex.Message);
            }
        }

        private void RemoveItem(ObservableCollection<FileItem> fileItems, FileItem fileItem)
        {
            foreach (FileItem item in fileItems)
            {
                if (item == fileItem)
                {
                    fileItems.Remove(item);
                    return;
                }
                if (item.File is StorageFolder)
                {
                    RemoveItem(item.Children, fileItem);
                }
            }
        }

        private async void Rename_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fi = (sender as FrameworkElement).DataContext as FileItem;
                string newname = await rename(fi.Type, fi.FileName);

                await fi.File.RenameAsync(newname, NameCollisionOption.GenerateUniqueName);
                fi.FileName = newname;
                if (fi.File is StorageFile sf)
                    fi.FileLanguage = FileItem.GetFileType(sf.FileType);
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async Task<string> rename(FileItem.ExplorerItemType type, string startstring)
        {
            var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Rename " + type.ToString().ToLower(), PrimaryButtonText = "rename", DefaultButton = ContentDialogButton.Primary };
            TextBox tb = new TextBox() { Text = startstring };
            cd.Content = tb;
            var res = await cd.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                App.VM.Log($"Renaming {type.ToString().ToLower()} {startstring} to {tb.Text}");
                return tb.Text;
            }
            else
                return startstring;
        }

        private void EditorTabViewDrag(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Open File";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        private async void EditorTabViewDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                foreach (StorageFile file in items)
                {
                    var fileitem = new FileItem(file) { };
                    VM.OpenFile(fileitem);
                }
            }
            else
            {
                foreach (var item in DraggedItems)
                {
                    if (item.Type == FileItem.ExplorerItemType.File)
                        VM.OpenFile(item);
                }
                DraggedItems.Clear();
            }
            e.Handled = true;
        }

        private async void FileCopy(DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems) && App.VM.IsProjectLoaded)
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                foreach (StorageFile file in items)
                {
                    var fi = new FileItem(file);
                    App.VM.CurrentProject.Directory.Add(fi);

                    if (Path.GetDirectoryName(file.Path) != Path.GetDirectoryName(App.VM.CurrentProject.Folder.Path))
                    {
                        await file.CopyAsync(App.VM.CurrentProject.Folder, file.Name, NameCollisionOption.GenerateUniqueName);
                        fi.FileFolder = Path.GetDirectoryName(file.Path);
                    }
                }
            }
        }

        private void TreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            try
            {
                DraggedItems.Clear();
                foreach (FileItem item in args.Items)
                {
                    DraggedItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                App.VM.Log("Error on TreeView_DragItemsStarting: " + ex.Message);
            }
        }

        private async void FileDrop(DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                foreach (StorageFile file in items)
                {
                    var fileitem = new FileItem(file) { };
                    App.VM.OpenFile(fileitem);
                }
            }
        }

        string[] LanguagesToOpen = { ".tex", "csv", ".lua", ".txt", ".md", ".html", ".htm", ".xml", ".log", ".js", ".json", ".xml", ".mkiv", ".mkii", ".mkxl", ".mkvi" };
        string[] BitmapsToOpen = { ".png", ".bmp", ".jpg", ".gif", ".tif", ".ico", ".svg" };

        private async void FileItemTapped(object sender, TappedRoutedEventArgs e)
        {
            MyTreeViewItem treeviewitem = sender as MyTreeViewItem;
            FileItem fileitem = treeviewitem.DataContext as FileItem;

            if (fileitem.Type == FileItem.ExplorerItemType.File)
            {
                string type = ((StorageFile)fileitem.File).FileType.ToLower();
                if (LanguagesToOpen.Contains(type))
                {
                    App.VM.OpenFile(fileitem);
                }
                else if (BitmapsToOpen.Contains(type))
                {
                    await Launcher.LaunchFileAsync(fileitem.File as StorageFile);
                    //var tip = new TeachingTip() { XamlRoot = XamlRoot, Title = fileitem.FileName, Target = (FrameworkElement)sender, PreferredPlacement = TeachingTipPlacementMode.RightTop, IsLightDismissEnabled = true, IsOpen = true };
                    //var content = new Grid() { Background = new SolidColorBrush(Colors.LightGray) };
                    //content.Children.Add(new Image() { Source = await LoadImage((StorageFile)fileitem.File), });
                    //tip.HeroContent = content;
                    //tip.HeroContentPlacement = TeachingTipHeroContentPlacementMode.Bottom;
                    //RootGrid.Children.Add(tip);

                    //tip.IsOpen = true;
                }
                else if (type == ".pdf")
                {
                    await OpenPDF((StorageFile)fileitem.File);
                }
            }
            e.Handled = true;
        }

        private static async Task<BitmapImage> LoadImage(StorageFile file)
        {
            BitmapImage bitmapImage = new BitmapImage();
            FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);
            bitmapImage.SetSource(stream);
            return bitmapImage;
        }

        private void SetRoot_Click(object sender, RoutedEventArgs e)
        {
            var ei = (FileItem)(sender as FrameworkElement).DataContext;
            if (ei.Level == 0)
            {
                ei.IsRoot = true;
                App.VM.CurrentProject.RootFile = ei.FileName;
            }
            else
            {
                VM.InfoMessage(true, "Warning", "The main file must be in the root folder of the project", InfoBarSeverity.Warning);
            }
        }

        #endregion

        #region Compiler Operations

        public async void CompileTex(bool compileRoot = false, FileItem fileToCompile = null)
        {
            if (!App.VM.IsSaving)
                try
                {
                    App.VM.IsError = false;
                    App.VM.IsPaused = false;
                    App.VM.IsVisible = true;
                    App.VM.IsSaving = true;

                    //await Task.Delay(500);

                    string[] modes = new string[] { };
                    if (App.VM.CurrentProject != null)
                        modes = App.VM.CurrentProject.Modes.Where(x => x.IsSelected).Select(x => x.Name).ToArray();
                    if (modes.Length > 0 && App.VM.CurrentProject.UseModes)
                        App.VM.Default.Modes = string.Join(",", modes);
                    else App.VM.Default.Modes = "";

                    FileItem filetocompile = null;
                    if (compileRoot)
                    {
                        FileItem[] root = new FileItem[] { };
                        if (App.VM.CurrentProject != null)
                            root = App.VM.CurrentProject.Directory.FirstOrDefault()?.Children?.Where(x => x.IsRoot)?.ToArray();
                        if (root != null && root.Length > 0)
                            filetocompile = root.FirstOrDefault();
                        else
                            filetocompile = fileToCompile ?? App.VM.CurrentFileItem;
                    }
                    else
                    {
                        filetocompile = fileToCompile ?? App.VM.CurrentFileItem;
                    }
                    
                    App.VM.Default.TexFileFolder = filetocompile.FileFolder;
                    App.VM.Default.TexFileName = filetocompile.FileName;
                    App.VM.Default.TexFilePath = filetocompile.File.Path;

                    bool compileSuccessful = false;

                    try
                    {
                        string param = "";
                        if (VM.Default.Modes.Length > 0)
                            param = "--mode=" + VM.Default.Modes + " ";

                        if (VM.IsProjectLoaded)
                        {
                            if (VM.CurrentProject.UseInterface)
                                param += "--interface=" + VM.CurrentProject.Interface + " ";

                            if (VM.CurrentProject.UseNonStopMode)
                                param += "--nonstopmode ";
                            if (VM.CurrentProject.UseNoConsole)
                                param += "--noconsole ";
                            if (VM.CurrentProject.UseParameters)
                                param += VM.CurrentProject.AdditionalParameters + " ";
                        }

                        string logtext = "Running compiler job: " + "context " + param + VM.Default.TexFileName;
                        await App.VM.Log(logtext);

                        await Task.Run(async () => { await Compile(param); });
                        compileSuccessful = true;
                    }
                    catch
                    {
                        compileSuccessful = false;
                    }

                    if (compileSuccessful)
                    {
                        string local = ApplicationData.Current.LocalFolder.Path;
                        string curFile = Path.GetFileName(App.VM.Default.TexFilePath);
                        string filewithoutext = Path.GetFileNameWithoutExtension(curFile);
                        string curPDF = filewithoutext + ".pdf";
                        string curPDFPath = Path.Combine(App.VM.Default.TexFilePath, curPDF);
                        string newPathToFile = Path.Combine(local, curPDF);
                        StorageFolder currFolder = await StorageFolder.GetFolderFromPathAsync(App.VM.Default.TexFileFolder);

                        var error = await currFolder.TryGetItemAsync(Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + "-error.log");
                        if (error != null)
                        {
                            VM.IsError = true;
                            StorageFile errorfile = error as StorageFile;
                            string text = await FileIO.ReadTextAsync(errorfile);

                            // BAD code, quick hack to convert the lua table to a json format. Depending on special characters in the error message, the JsonConvert.DeserializeObject function can through errors.
                            string newtext = text.Replace("  ", "").Replace("return", "").Replace("[\"", "\"").Replace("\"]", "\"").Replace("=", ":");
                            string pattern = @"([^\\])(\\n)"; // Matches every \n except \\n
                            string replacement = "$1"; // Match gets replaced with first capturing group, e.g. ]\n --> ]
                            newtext = Regex.Replace(newtext, pattern, replacement);

                            var errormessage = JsonConvert.DeserializeObject<ConTeXtErrorMessage>(newtext);

                            VM.Log("Compiler error: " + errormessage.lasttexerror);
                            VM.ConTeXtErrorMessage = errormessage;
                            FileItem erroritem = VM.CurrentProject.GetFileItemByName(VM.CurrentProject.Directory[0],VM.ConTeXtErrorMessage.filename.Replace("./",""));
                            if (erroritem != null)
                            {
                                VM.OpenFile(erroritem);
                                await Task.Delay(500);
                                List<SyntaxError> errors = new();
                                try
                                {
                                    errors.Add(new() { SyntaxErrorType = SyntaxErrorType.Error, iLine = VM.ConTeXtErrorMessage.linenumber - 1, Title = VM.ConTeXtErrorMessage.lasttexerror });

                                    VM.Codewriter.SyntaxErrors = errors;
                                    VM.Codewriter.SelectLine(new(0, VM.ConTeXtErrorMessage.linenumber - 1));
                                    VM.Codewriter.CenterView();
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            VM.IsPaused = true;
                            VM.IsError = false;
                            VM.IsVisible = false;
                        }

                        if (VM.Default.AutoOpenPDF && error == null)
                        {
                            StorageFile pdfout = await currFolder.TryGetItemAsync(Path.GetFileNameWithoutExtension(VM.Default.TexFileName) + ".pdf") as StorageFile;
                            if (pdfout != null)
                            {
                                VM.Log("Opening " + System.IO.Path.GetFileNameWithoutExtension(VM.Default.TexFileName) + ".pdf");
                                if (VM.Default.InternalViewer)
                                {
                                    await OpenPDF(pdfout);
                                }
                                else
                                {
                                    await Launcher.LaunchFileAsync(pdfout);
                                }
                            }
                        }

                        if (App.VM.Default.AutoOpenLOG)
                        {
                            if ((App.VM.Default.AutoOpenLOGOnlyOnError && error != null) | !App.VM.Default.AutoOpenLOGOnlyOnError)
                            {
                                StorageFile logout = await currFolder.TryGetItemAsync(Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".log") as StorageFile;
                                if (logout != null)
                                {
                                    FileItem logFile = new FileItem(logout) { };
                                    App.VM.OpenFile(logFile);
                                }
                            }
                            else if (App.VM.Default.AutoOpenLOGOnlyOnError && error == null)
                            {
                                if (App.VM.FileItems.Any(x => x.IsLogFile))
                                {
                                    App.VM.FileItems.Remove(App.VM.FileItems.First(x => x.IsLogFile));
                                }
                            }
                        }
                    }
                    VM.Codewriter.Focus(FocusState.Keyboard);
                }
                catch (Exception f)
                {
                    App.VM.Log("Exception at compile: " + f.Message);
                }
            App.VM.IsSaving = false;
        }

       

        private async Task<bool> OpenPDF(StorageFile pdfout)
        {
            try
            {
                Stream stream = await pdfout.OpenStreamForReadAsync();
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                var asBase64 = Convert.ToBase64String(buffer);
                VM.IsInternalViewerActive = true;
                await PDFReader.ExecuteScriptAsync("window.openPdfAsBase64('" + asBase64 + "')");

                return true;
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
                VM.IsInternalViewerActive = false;
                return false;
            }
        }

        private void PDFReader_NewWindowRequested(WebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
        {
            args.Handled = true;
        }

        private async void Compile_Click(object sender, RoutedEventArgs e)
        {
            FileItem fi = (sender as FrameworkElement).DataContext as FileItem;
            await VM.SaveAll();
            VM.Codewriter.Save();
            CompileTex(false, fi);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await VM.Save((sender as FrameworkElement).DataContext as FileItem);
            VM.Codewriter.Save();
        }

        private async Task<bool> InstallContext()
        {
            try
            {
                var storageFolder = ApplicationData.Current.LocalFolder;
                VM.Default.ContextDistributionPath = storageFolder.Path; 

                var p = Path.Combine(Package.Current.Installed­Location.Path, @"ConTeXt-IDE.Desktop", @"context-mswin.zip");

                StorageFile zipfile;

                if (File.Exists(p)) {
                    zipfile = await StorageFile.GetFileFromPathAsync(p);
                }
                else
                {
                    zipfile = await StorageFile.GetFileFromApplicationUriAsync(new("ms-appx:///context-mswin.zip"));
                }

                ZipFile.ExtractToDirectory(zipfile.Path, storageFolder.Path, true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                App.write(ex.Message + "\n" + ex.StackTrace);
                return false;
            }
        }

        private string getversion()
        {
            if (Directory.Exists(VM.Default.ContextDistributionPath + @"\tex\texmf-win64"))
            {
                return @"\texmf-win64";
            }
            else if (Directory.Exists(VM.Default.ContextDistributionPath + @"\tex\texmf-mswin"))
            {
                return @"\texmf-mswin";
            }
            else
                return @"\texmf-mswin";
        }

        public async Task<bool> Compile(string param)
        {
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo(@"C:\Windows\System32\cmd.exe")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                WorkingDirectory = VM.Default.TexFileFolder
            };
            p.OutputDataReceived += (e, f) =>
            { //VM.Log(f.Data.);
            };
            //p.ErrorDataReceived += (e, f) => {Log(f.Data); };
            p.StartInfo = info;
            p.Start();
            p.BeginOutputReadLine();
            //using (StreamReader sr = p.StandardOutput)
            using (StreamWriter sw = p.StandardInput)
            {
                

                sw.WriteLine(VM.Default.ContextDistributionPath + @"\tex" + getversion() + @"\bin\context.exe" + " " + param + VM.Default.TexFileName);

            }

            p.WaitForExit();
            //string output = p.StandardOutput.ReadToEnd();

            if (!File.Exists(VM.Default.TexFilePath + @"-error.log"))
            {
                VM.Log("Compiler process successfully compleated.");
                return true;
            }
            else
            {
                VM.Log("Compiler process terminated with errors.");
                return false;
            }
        }

        #endregion

        #region Editor & Viewer

        private async void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            try
            {
                var fi = args.Tab.DataContext as FileItem;
                if (fi.IsChanged)
                {
                    var save = new ContentDialog() { XamlRoot = XamlRoot, Title = "Do you want to save this file before closing?", PrimaryButtonText = "Yes", SecondaryButtonText = "No", DefaultButton = ContentDialogButton.Primary };

                    if (await save.ShowAsync() == ContentDialogResult.Primary)
                    {
                        await App.VM.Save(fi);
                    }
                }
                if (VM.FileItems.Contains(fi))
                    VM.FileItems.Remove(fi);

                //VM.CurrentProject.LastOpenedFiles = VM.FileItems.Select(x => x.FileName).ToList();
            }
            catch (Exception ex)
            {
                await VM.Log(ex.Message);
            }
        }

        private async void Pin_Click(object sender, RoutedEventArgs e)
        {
            var fi = (FileItem)(sender as FrameworkElement).DataContext;

            if (!fi.IsPinned)
            {
                bool reselect = fi == VM.CurrentFileItem;
                VM.FileItems.Remove(fi);
                VM.FileItems.Insert(0, fi);
                if (reselect)
                {
                    await Task.Delay(500);
                    VM.CurrentFileItem = fi;
                }
            }

            fi.IsPinned = !fi.IsPinned;
        }

        #endregion

        #region UI Updates

        public void ShowTeachingTip(string ID, object Target)
        {
            return; // Teaching Tips are busted in WinAppSDK 1.0
            if (VM.Default.HelpItemList.Any(x => x.ID == ID))
            {
                var helpItem = VM.Default.HelpItemList[VM.Default.HelpItemList.IndexOf(VM.Default.HelpItemList.First(x => x.ID == ID))];
                if (!helpItem.Shown)
                {
                    var tip = new TeachingTip() { XamlRoot = XamlRoot, Title = helpItem.Title, Target = (FrameworkElement)Target, PreferredPlacement = TeachingTipPlacementMode.Right, IsLightDismissEnabled = false, IsOpen = false };
                    if (!string.IsNullOrWhiteSpace(helpItem.Subtitle))
                        tip.Subtitle = helpItem.Subtitle;
                    tip.Content = new TextBlock() { TextWrapping = TextWrapping.WrapWholeWords, Text = helpItem.Text };
                    RootGrid.Children.Add(tip);
                    tip.IsOpen = true;
                    helpItem.Shown = true;
                    VM.Default.SaveSettings();
                }
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            Log.Blocks.Clear();
            RichTextBlockHelper.logline = 0;
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ShowLog":
                    if (App.VM.Default.ShowLog)
                    {
                        IDEGridRow.Height = new GridLength(2, GridUnitType.Star);
                        LogGridSplitter.Height = new GridLength(6, GridUnitType.Pixel);
                        LogGridRow.Height = new GridLength(200, GridUnitType.Pixel);
                    }
                    else
                    {
                        IDEGridRow.Height = new GridLength(1, GridUnitType.Star);
                        LogGridSplitter.Height = new GridLength(0, GridUnitType.Pixel);
                        LogGridRow.Height = new GridLength(0, GridUnitType.Pixel);
                    }
                    break;

                case "ShowProjects":
                    if (App.VM.Default.ShowProjects)
                    {
                        ProjectsGridSplitter.Height = new GridLength(6, GridUnitType.Pixel);
                        ProjectsGridLibraryRow.Height = new GridLength(300, GridUnitType.Pixel);
                    }
                    else
                    {
                        ProjectsGridSplitter.Height = new GridLength(0, GridUnitType.Pixel);
                        ProjectsGridLibraryRow.Height = new GridLength(0, GridUnitType.Pixel);
                    }
                    break;

                case "ShowProjectPane":
                    if (App.VM.Default.ShowProjectPane)
                    {
                        ContentGridSplitter.Width = new GridLength(6, GridUnitType.Pixel);
                        ContentGridProjectPaneColumn.Width = new GridLength(300, GridUnitType.Pixel);
                    }
                    else
                    {
                        ContentGridSplitter.Width = new GridLength(0, GridUnitType.Pixel);
                        ContentGridProjectPaneColumn.Width = new GridLength(0, GridUnitType.Pixel);
                    }
                    break;

                case "InternalViewer":
                    if (App.VM.Default.InternalViewer)
                    {
                        PDFGridSplitter.Width = new GridLength(6, GridUnitType.Pixel);
                        PDFGridColumn.Width = new GridLength(400, GridUnitType.Pixel);
                    }
                    else
                    {
                        PDFGridSplitter.Width = new GridLength(0, GridUnitType.Pixel);
                        PDFGridColumn.Width = new GridLength(0, GridUnitType.Pixel);
                    }
                    break;
                default: break;
            }
        }

        private async void Edit_PositionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
            }
            catch
            {

            }
        }

        #endregion

        #region ConTeXt Projects

        private async void Btnaddproject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectNew = new SelectNew() { XamlRoot = XamlRoot };
                var selectTemplate = new SelectTemplate() { XamlRoot = XamlRoot };
                var selectFolder = new SelectFolder() { XamlRoot = XamlRoot };

                var result = await selectNew.ShowAsync();
                ContentDialogResult res;
                if (result == ContentDialogResult.Primary)
                {
                    switch ((selectNew.TempList.SelectedItem as TemplateSelection).Tag)
                    {
                        case "empty":
                            res = await selectFolder.ShowAsync();
                            if (res == ContentDialogResult.Primary)
                            {
                                var folder = selectFolder.folder;
                                if (folder != null)
                                {
                                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(folder.Name, folder, "");
                                    StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(folder.Name, folder, "");
                                    App.VM.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;

                                    App.VM.FileItemsTree.Clear();
                                    var proj = new Project(folder.Name, folder, App.VM.FileItemsTree);
                                    App.VM.Default.ProjectList.Add(proj);
                                    App.VM.CurrentProject = proj;
                                    await App.VM.GenerateTreeView(folder);

                                    App.VM.Default.LastActiveProject = proj.Name;
                                    // App.AppViewModel.UpdateRecentAccessList();
                                }
                            }
                            break;

                        case "template":
                            res = await selectTemplate.ShowAsync();
                            if (res == ContentDialogResult.Primary)
                            {
                                string project = (selectTemplate.TempList.SelectedItem as TemplateSelection).Tag;

                                res = await selectFolder.ShowAsync();
                                if (res == ContentDialogResult.Primary)
                                {
                                    var folder = selectFolder.folder;
                                    if (folder != null)
                                    {
                                        StorageApplicationPermissions.FutureAccessList.AddOrReplace(folder.Name, folder, "");
                                        StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(folder.Name, folder, "");
                                        App.VM.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                                        string root = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                                        string path = root + @"\Templates";
                                        var templateFolder = await StorageFolder.GetFolderFromPathAsync(path + @"\" + project);

                                        await CopyFolderAsync(templateFolder, folder);
                                        string rootfile = "";
                                        switch (project)
                                        {
                                            case "mwe": rootfile = "main.tex"; break;
                                            case "projpres": rootfile = "prd_presentation.tex"; break;
                                            case "projthes": rootfile = "prd_thesis.tex"; break;
                                            case "single": rootfile = "main.tex"; break;
                                            default: break;
                                        }

                                        StorageApplicationPermissions.FutureAccessList.AddOrReplace(folder.Name, folder, "");
                                        StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(folder.Name, folder, "");
                                        App.VM.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;

                                        App.VM.FileItemsTree.Clear();
                                        var proj = new Project(folder.Name, folder, App.VM.FileItemsTree);
                                        proj.RootFile = rootfile;
                                        App.VM.Default.ProjectList.Add(proj);
                                        App.VM.CurrentProject = proj;
                                       await  App.VM.GenerateTreeView(folder, rootfile);

                                        App.VM.Default.LastActiveProject = proj.Name;

                                        App.VM.OpenFile(App.VM.CurrentRootItem);
                                    }
                                }
                            }
                            break;

                        default: break;
                    }
                }
            }
            catch (Exception ex)
            {
                App.VM.Log("Exception on adding Project: " + ex.Message);
            }
        }

        private void Btndeleteproject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var proj = (sender as FrameworkElement).DataContext as Project;
                StorageApplicationPermissions.MostRecentlyUsedList.Remove(proj.Name);
                App.VM.Default.ProjectList.Remove(proj);
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error on deleting project", ex.Message, InfoBarSeverity.Error);
            }
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var proj = (sender as FrameworkElement).DataContext as Project;
                await VM.LoadProject(proj);
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error on loading project", ex.Message, InfoBarSeverity.Error);
            }
        }

        private void Unload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.VM.CurrentProject = new Project();
                App.VM.FileItems.Clear();
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        #endregion

        #region Ribbon : About

        private void Disclaimer_Click(object sender, RoutedEventArgs e)
        {
            DisclaimerView.Visibility = DisclaimerView.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            await AboutDialog.ShowAsync();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                App.VM.IsSaving = true;
                VM.InfoMessage(true, "Updating", "Your ConTeXt distribution is getting updated. This can take up to 10 minutes.", InfoBarSeverity.Informational);

                bool UpdateSuccessful = false;
                await Task.Run(async () => { UpdateSuccessful = await Update(); });

                if (UpdateSuccessful)
                {
                    VM.InfoMessage(true, "Update complete!", "Your ConTeXt distribution is up n' running!", InfoBarSeverity.Success);
                }
                else
                {
                    VM.InfoMessage(true, "Error", "Something went wrong. Please try again later.", InfoBarSeverity.Error);
                }
            }
            else
                VM.InfoMessage(true, "No internet connection", "You need to be connected to the internet in order to update your ConTeXt distribution!", InfoBarSeverity.Warning);

            VM.IsSaving = false;
        }

        private async Task<bool> Update()
        {
            try
            {
                Process p = new Process();
                p.EnableRaisingEvents = true;
                ProcessStartInfo info = new ProcessStartInfo(@"C:\Windows\System32\cmd.exe")
                {

                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    WorkingDirectory = VM.Default.ContextDistributionPath
                };
                p.OutputDataReceived += (e, f) =>
                {// VM.Log(f.Data);
                };
                p.StartInfo = info;
                p.Start();
                p.BeginOutputReadLine();

                using (StreamWriter sw = p.StandardInput)
                {
                    sw.WriteLine(@"install.bat --modules=all");
                    //sw.WriteLine("setx path \"%PATH%;" + VM.Default.ContextDistributionPath + @"\tex\texmf-mswin\bin" + "\"");
                }
                p.WaitForExit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void Rate_Click(object sender, RoutedEventArgs e)
        {
            bool result = await Launcher.LaunchUriAsync(new System.Uri("ms-windows-store://review/?ProductId=9nn9q389ttjr"));
        }

        private async void Github_Click(object sender, RoutedEventArgs e)
        {
            bool result = await Launcher.LaunchUriAsync(new System.Uri("https://github.com/WelterDevelopment/ConTeXt-IDE-WinUI"));
        }

        #endregion

        #region Ribbon : Start

        private async void Btncompile_Click(object sender, RoutedEventArgs e)
        {
            await VM.SaveAll();
            VM.Codewriter.Save();
            CompileTex();
        }

        private async void Btncompileroot_Click(object sender, RoutedEventArgs e)
        {
            await VM.SaveAll();
            VM.Codewriter.Save();
            CompileTex(true);
        }


        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            VM.Codewriter.TextAction_Undo();
            VM.Codewriter.Focus( FocusState.Keyboard);
        }

        private async void Btnsave_Click(object sender, RoutedEventArgs e)
        {
            await VM.Save();
            VM.Codewriter.Save();
        }

        private async void btnsaveall_Click(object sender, RoutedEventArgs e)
        {
            await VM.SaveAll();
            VM.Codewriter.Save();
        }

        private void Modes_Click(object sender, RoutedEventArgs e)
        {
            ShowTeachingTip("Modes", sender);
        }

        private void Environments_Click(object sender, RoutedEventArgs e)
        {
            ShowTeachingTip("Environments", sender);
        }
        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            StorageApplicationPermissions.MostRecentlyUsedList.Clear();
            VM.Default.ProjectList.Clear();
            Settings.RestoreSettings();
            Unload_Click(null,null);
            
           
        }

        private async void addMode_Click(object sender, RoutedEventArgs e)
        {
            Mode mode = new Mode();
            string name = "";
            var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Add mode", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
            TextBox tb = new TextBox() { Text = name };
            cd.Content = tb;
            if (await cd.ShowAsync() == ContentDialogResult.Primary)
            {
                mode.IsSelected = true;
                mode.Name = tb.Text;
                App.VM.CurrentProject.Modes.Add(mode);
                App.VM.Default.SaveSettings();
            }
        }

        private async void addEnvironment_Click(object sender, RoutedEventArgs e)
        {
            Mode mode = new Mode();
            string name = "";
            var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Add environment", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
            TextBox tb = new TextBox() { Text = name };
            cd.Content = tb;
            if (await cd.ShowAsync() == ContentDialogResult.Primary)
            {
                mode.IsSelected = true;
                mode.Name = tb.Text;
                VM.CurrentProject.Environments.Add(mode);
                VM.Default.SaveSettings();
            }
        }

        private void RemoveMode_Click(object sender, RoutedEventArgs e)
        {
            Mode m = (sender as FrameworkElement).DataContext as Mode;
            VM.CurrentProject.Modes.Remove(m);
            VM.Default.SaveSettings();
        }

        private void RemoveEnvironment_Click(object sender, RoutedEventArgs e)
        {
            Mode m = (sender as FrameworkElement).DataContext as Mode;
            VM.CurrentProject.Environments.Remove(m);
            VM.Default.SaveSettings();
        }

        #endregion

        #region Ribbon : Editor

        private async void Help_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                var hf = e.ClickedItem as Helpfile;
                var lsf = ApplicationData.Current.LocalFolder;
                App.VM.Log("Opening " + lsf.Path + hf.Path + hf.FileName);
                var sf = await StorageFile.GetFileFromPathAsync(lsf.Path + hf.Path + hf.FileName);

                if (App.VM.Default.HelpPDFInInternalViewer)
                {
                    await OpenPDF(sf);
                }
                else
                    await Launcher.LaunchFileAsync(sf);
            }
            catch (Exception ex)
            {
                App.VM.Log("Exception on Help Item Click: " + ex.Message);
            }
        }

        private void Btn_FontSize_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag.ToString())
            {
                case "FontSizeUp": VM.Default.FontSize++; break;
                case "FontSizeDown": VM.Default.FontSize--; break;
            }
        }
        private void Btn_Exit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Exit();
        }
        private void Btn_Minimize_Click(object sender, RoutedEventArgs e)
        {
            //App.Current.mini();
        }

        private void Btn_FontSize_Holding(object sender, HoldingRoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag.ToString())
            {
                case "FontSizeUp": VM.Default.FontSize++; break;
                case "FontSizeDown": VM.Default.FontSize--; break;
            }
            e.Handled = true;
        }

        private void Tbx_FontSize_Wheel(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(sender as UIElement);
            int wheeldelta = point.Properties.MouseWheelDelta;
            if (wheeldelta > 0)
            {
                VM.Default.FontSize++;
            }
            else
            {
                VM.Default.FontSize--;
            }
        }

        #endregion

        #region Ribbon : View

        private void ColorsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is AccentColor accentColor)
            {
                SetColor(accentColor);
                VM.Default.AccentColor = accentColor.Name;
            }
        }

        public void SetColor(AccentColor accentColor = null, bool reload = true)
        {
            var setting = ((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]);
            if (accentColor != null) setting.AccentColor = accentColor.Color;
            ColorPaletteResources palette = new ColorPaletteResources();
            palette.Accent = setting.AccentColorLow;
            App.Current.Resources.MergedDictionaries.Add(palette);
            if (reload)
                ReloadPageTheme(this.RequestedTheme);
            if (App.mainWindow.IsCustomizationSupported)
            {
                App.mainWindow.AW.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                App.mainWindow.AW.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                App.mainWindow.AW.TitleBar.ButtonHoverBackgroundColor = setting.AccentColor;
            }
        }

        private ColorPaletteResources FindColorPaletteResourcesForTheme(string theme)
        {
            foreach (var themeDictionary in Application.Current.Resources.ThemeDictionaries)
            {
                if (themeDictionary.Key.ToString() == theme)
                {
                    if (themeDictionary.Value is ColorPaletteResources)
                    {
                        return themeDictionary.Value as ColorPaletteResources;
                    }
                    else if (themeDictionary.Value is ResourceDictionary targetDictionary)
                    {
                        foreach (var mergedDictionary in targetDictionary.MergedDictionaries)
                        {
                            if (mergedDictionary is ColorPaletteResources)
                            {
                                return mergedDictionary as ColorPaletteResources;
                            }
                        }
                    }
                }
            }
            return null;
        }
        private async void ReloadPageTheme(ElementTheme startTheme)
        {
            if (this.RequestedTheme == ElementTheme.Dark)
                VM.Default.Theme = "Light";
            else if (this.RequestedTheme == ElementTheme.Light)
                VM.Default.Theme = "Default";
            else if (this.RequestedTheme == ElementTheme.Default)
                VM.Default.Theme = "Dark";

            //await Task.Delay(5000);

            if (this.RequestedTheme != startTheme)
                ReloadPageTheme(startTheme);
        }

        private void ColorReset_Click(object sender, RoutedEventArgs e)
        {
            if (VM.AccentColors.Any(x => x.Color == VM.SystemAccentColor))
            {
                VM.AccentColor = VM.AccentColors.First(x => x.Color == VM.SystemAccentColor);
                SetColor(VM.AccentColor);
            }
            else
            {
                VM.AccentColor =null;
                SetColor(new("Default", VM.SystemAccentColor));
            }
            
            VM.Default.AccentColor = "Default";
        }

        #endregion

        #region ConTeXt

        private async void Btn_InstallModule_Click(object sender, RoutedEventArgs e)
        {
            ContextModule module = (sender as FrameworkElement).DataContext as ContextModule;

            if (NetworkInterface.GetIsNetworkAvailable())
                DownloadModule(module);
        }

        private async void Btn_RemoveModule_Click(object sender, RoutedEventArgs e)
        {
            VM.IsIndeterminate = true;
            VM.IsSaving = true;
            ContextModule module = (sender as FrameworkElement).DataContext as ContextModule;

            string modulepath = Path.Combine(ApplicationData.Current.LocalFolder.Path, @"tex\texmf\tex\context\third\", module.Name + @"\");

            if (Directory.Exists(modulepath))
            {
                await (await StorageFolder.GetFolderFromPathAsync(modulepath)).DeleteAsync();
            }

            module.IsInstalled = false;

            VM.Default.SaveSettings();

            await Generate();
            VM.IsSaving = false;
        }

        private void DownloadModule(ContextModule module)
        {
            try
            {
                VM.IsIndeterminate = true;
                VM.IsSaving = true;
                using (WebClient wc = new WebClient())
                {
                    string filepath = Path.Combine(ApplicationData.Current.LocalFolder.Path, module.Name + ".zip");

                    if (File.Exists(filepath))
                    {
                        File.Delete(filepath);
                    }

                    bool InstallSuccessful = false;
                    wc.DownloadProgressChanged += (a, b) => { VM.ProgressValue = b.ProgressPercentage; };
                    wc.DownloadFileCompleted += async (a, b) =>
                    {
                        VM.IsIndeterminate = true;

                        InstallSuccessful = await InstallModule(module, filepath);

                        VM.IsSaving = false;

                        module.IsInstalled = true;


                        VM.Default.SaveSettings();
                    };
                    wc.DownloadFileAsync(new System.Uri(module.URL), filepath);
                }
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Download Error", ex.Message, InfoBarSeverity.Error);
            }


        }

        private async Task<bool> InstallModule(ContextModule module, string filepath)
        {
            try
            {
                string installpath = "";
                switch (module.Type)
                {
                    case ContextModuleType.TDSArchive:
                        installpath = @"tex\texmf\";
                        break;
                    case ContextModuleType.Archive:
                        installpath = @"tex\texmf\";
                        break;
                    case ContextModuleType.GitHub:
                        installpath = @"tex\texmf\";
                        break;
                }

                string extractionpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, installpath);

                ZipFile.ExtractToDirectory(filepath, extractionpath, true);

                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }

                if (module.Type == ContextModuleType.Archive)
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(extractionpath, module.ArchiveFolderPath));
                    // VM.InfoMessage(true, folder.Path);
                    var extractionfolder = await StorageFolder.GetFolderFromPathAsync(extractionpath);
                    // VM.InfoMessage(true, extractionfolder.Path);

                    await CopyFolderAsync(folder, extractionfolder);

                }



                return await Generate();
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Install Error", ex.Message, InfoBarSeverity.Error);
                return false;
            }
        }

        private async Task<bool> Generate()
        {
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo(@"C:\Windows\System32\cmd.exe")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                WorkingDirectory = VM.Default.ContextDistributionPath
            };
            //p.OutputDataReceived += (e, f) =>
            //{ //VM.Log(f.Data.);
            //};
            //p.ErrorDataReceived += (e, f) => {Log(f.Data); };
            p.StartInfo = info;
            p.Start();
            p.BeginOutputReadLine();
            //using (StreamReader sr = p.StandardOutput)
            using (StreamWriter sw = p.StandardInput)
            {
                sw.WriteLine(VM.Default.ContextDistributionPath + @"\tex" + getversion() + @"\bin\context.exe" + " --make ");
                sw.WriteLine(VM.Default.ContextDistributionPath + @"\tex" + getversion() + @"\bin\context.exe" + " --generate ");

            }

            p.WaitForExit();

            return p.ExitCode == 0;

        }

        #endregion
        private List<Command> contextcommands;
        private async void InitializeCommandReference()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Interface), "cd");
                string xml = File.ReadAllText(Path.Combine(ApplicationData.Current.LocalFolder.Path, @"tex\texmf-context\tex\context\interface\mkiv\context-en.xml"));
                using (StringReader reader = new StringReader(xml))
                {
                    Interface contextinterface = (Interface)serializer.Deserialize(reader);
                    List<Command> commands = contextinterface.InterfaceList.SelectMany(x => x.Command).ToList();
                    List<Command> commandstoiterate = new(commands);

                    foreach (var instancecommand in commandstoiterate.Where(x => x.Variant?.ToLower() == "instance"))
                    {
                        if (instancecommand?.Instances?.Constant != null)
                        {
                            foreach (var command in instancecommand?.Instances?.Constant)
                            {
                                Command newcommand = (Command)instancecommand.Clone();
                                newcommand.Name = command?.Value;
                                commands.Insert(commands.IndexOf(instancecommand),newcommand);
                            }
                            commands.Remove(instancecommand);
                        }
                    }

                    contextcommands = commands;

                    foreach (Command command in contextcommands)
                    {
                        if (command?.Arguments?.ArgumentsList != null)
                        {
                            int i = 0;
                            foreach (Argument argument in command?.Arguments?.ArgumentsList)
                            {
                                if (argument != null)
                                {
                                    i++;
                                    argument.Number = i;
                                }
                            }
                        }
                    }

                    IOrderedEnumerable<IGrouping<string, Command>> query = from item in contextcommands
                                                                           group item by item?.Category into g
                                                                           where !string.IsNullOrWhiteSpace(g.Key)
                                                                           orderby g.Key
                                                                           select g;

                    VM.ContextCommandGroupList = query.SelectMany(x => x).Select(x => x.Category).Distinct().ToList();

                    if (VM.Default.CommandGroups.Count == 0)
                    {
                        VM.Default.CommandGroups = VM.ContextCommandGroupList.Select(x => new CommandGroup() { Name = x }).ToList();
                    }
                    IOrderedEnumerable<IGrouping<string, Command>> filtered;
                    filtered = from item in query
                               where VM.Default.CommandGroups.Where(x => x.IsSelected).Select(x => x.Name).Contains(item.Key)
                               orderby item.Key
                               select item;

                    VM.ContextCommandGroupList = filtered.SelectMany(x => x).Select(x => x.Category).Distinct().ToList();
                    cvs.Source = filtered;

                    DocumentationView.SelectedIndex = -1;
                    PopulateIntelliSense("ConTeXt");
                }
            }
            catch (Exception ex)
            {
                VM.InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
            }

        }

        private void PopulateIntelliSense(string name)
        {
            var lang = FileLanguages.LanguageList.First(x => x.Name == name);
            if (lang.Commands == null)
                lang.Commands = new();
            foreach (Command command in contextcommands)
            {
                lang.Commands.Add(new(@"\" + (command.Type == "environment" ? "start" : "") + command.Name) { Snippet = (command.Type == "environment" ? "[]\n\t\n\\stop" +command.Name : ""), Description = "Category: " + command.Category });
            }
            if (VM.Codewriter?.Language?.Name == name)
                VM.Codewriter.Language.Commands = lang.Commands;
            VM.Codewriter?.UpdateSuggestions();            
        }

        private void GroupList_ItemClick(object sender, ItemClickEventArgs e)
        {
            GroupListView.Visibility = Visibility.Collapsed;
            DocumentationView.IsEnabled = true;
            DocumentationView.Visibility = Visibility.Visible;
            DocumentationView.ScrollIntoView((cvs.Source as IOrderedEnumerable<IGrouping<string, Command>>).SelectMany(x => x).First(x => x.Category == (string)e.ClickedItem), ScrollIntoViewAlignment.Leading);

        }

        private void Btn_GroupHeader_Click(object sender, RoutedEventArgs e)
        {
            DocumentationView.IsEnabled = false;
            DocumentationView.Visibility = Visibility.Collapsed;
            GroupListView.Visibility = Visibility.Visible;
            GroupListView.SelectedItem = ((sender as Button).DataContext as IGrouping<string, Command>).Key;
            GroupListView.ScrollIntoView(((sender as Button).DataContext as IGrouping<string, Command>).Key, ScrollIntoViewAlignment.Default);

        }

        private void Btn_CommandGroup_Click(object sender, RoutedEventArgs e)
        {
            InitializeCommandReference();
        }

        private bool IsSearching = false;
        private async void Searchtext_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!IsSearching)
                {
                    IsSearching = true;
                    IOrderedEnumerable<IGrouping<string, Command>> filtered = null;
                    string text = (sender as TextBox).Text;
                    await Task.Run(() => { filtered = UpdateSearchFilter(text); });

                    VM.ContextCommandGroupList = filtered.SelectMany(x => x).Select(x => x.Category).Distinct().ToList();
                    cvs.Source = filtered;

                    DocumentationView.SelectedIndex = -1;
                    IsSearching = false;
                }
            }
            catch (Exception ex)
            {
                VM.Log(ex.Message);
            }
        }

        private IOrderedEnumerable<IGrouping<string, Command>> UpdateSearchFilter(string text)
        {
            IOrderedEnumerable<IGrouping<string, Command>> query = from item in contextcommands
                                                                   where item.Name.Insert(0, @"\" + (item.Type == "environment" ? "start" : "")).Contains(text)
                                                                   group item by item?.Category into g
                                                                   where !string.IsNullOrWhiteSpace(g.Key)
                                                                   orderby g.Key
                                                                   select g;
            return from item in query
                   where VM.Default.CommandGroups.Where(x => x.IsSelected).Select(x => x.Name).Contains(item.Key)
                   orderby item.Key
                   select item;
        }

        private async void Btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            VM.IsSaving = true;
            await VM.ClearProject(VM.CurrentProject);
            VM.IsSaving = false;
        }

        private void Btn_CloseError_Click(object sender, RoutedEventArgs e)
        {
            VM.IsError = false;
            VM.IsVisible = false;
        }

        private async void Codewriter_ErrorOccured(object sender, ErrorEventArgs e)
        {
            Exception ex = e.GetException();
            await  VM.Log ($"Editor Message: {ex.Message}");
                await VM.Log($"\t {ex.StackTrace}");
        }
    }
}
