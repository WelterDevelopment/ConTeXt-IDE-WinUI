﻿using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Monaco;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
            }
            catch (Exception ex)
            {
                App.VM.InfoMessage(true,"Exception",ex.Message, InfoBarSeverity.Error);
            }
            
        }

        #region Page Load

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
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
        private async void Edit_Loading(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                try
                {
                    loaded = true;
                    var edit = sender as CodeEditor;
                    var languages = new LanguagesHelper(edit);
                    await languages.RegisterHoverProviderAsync("context", new EditorHoverProvider());
                    await languages.RegisterCompletionItemProviderAsync("context", new ContextCompletionProvider());
                    await edit.AddActionAsync(new RunAction());
                    await edit.AddActionAsync(new RunRootAction());
                    await edit.AddActionAsync(new SaveAction());
                    await edit.AddActionAsync(new SaveAllAction());
                    loaded = true;
                }
                catch (Exception ex)
                {
                    VM.InfoMessage(true,"Error on CodeEditor load", ex.Message, InfoBarSeverity.Error);
                }
            }
        }
        private void Edit_Unloaded(object sender, RoutedEventArgs e)
        {
            loaded = false;
        }

        private async void DisclaimerView_Loaded(object sender, RoutedEventArgs e)
        {
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new System.Uri("ms-appx:///web/About.html"));
            (sender as WebView2).Source = new System.Uri(storageFile.Path);
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

            await VM.Startup();
            FirstStart();
            OnProtocolActivated(Windows.ApplicationModel.AppInstance.GetActivatedEventArgs());
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
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new System.Uri("ms-appx:///web/viewer.html"));
            (sender as WebView2).Source = new System.Uri(storageFile.Path);
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


        public static async Task CopyFolderAsync(StorageFolder source, StorageFolder destinationContainer, string desiredName = null)
        {
            foreach (var folder in await source.GetFoldersAsync())
            {
                var innerfolder = await destinationContainer.CreateFolderAsync(folder.Name, CreationCollisionOption.GenerateUniqueName);
                await CopyFolderAsync(folder, innerfolder);
            }
            foreach (var file in await source.GetFilesAsync())
            {
                await file.CopyAsync(destinationContainer, file.Name, NameCollisionOption.GenerateUniqueName);
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
                VM.Log(ex.Message);
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
                App.VM.Log(ex.Message);
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

        string[] LanguagesToOpen = { ".tex", ".lua", ".md", ".html", ".xml", ".log", ".js", ".json", ".xml", ".mkiv", ".mkii", ".mkxl", ".mkvi" };
        string[] BitmapsToOpen = { ".png", ".bmp", ".jpg", ".gif", ".tif", ".ico", ".svg" };

        private async void FileItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var treeviewitem = (sender as MyTreeViewItem);
            var fileitem = treeviewitem.DataContext as FileItem;

            if (fileitem.Type == FileItem.ExplorerItemType.File)
            {
                string type = ((StorageFile)fileitem.File).FileType.ToLower();
                if (LanguagesToOpen.Contains(type))
                {
                    App.VM.OpenFile(fileitem);
                }
                else if (BitmapsToOpen.Contains(type))
                {
                    var tip = new TeachingTip() { Title = fileitem.FileName, Target = (FrameworkElement)sender, PreferredPlacement = TeachingTipPlacementMode.RightTop, IsLightDismissEnabled = true, IsOpen = false };
                    var content = new Grid() { Background = new SolidColorBrush(Colors.LightGray) };
                    content.Children.Add(new Image() { Source = await LoadImage((StorageFile)fileitem.File), });
                    tip.HeroContent = content;
                    tip.HeroContentPlacement = TeachingTipHeroContentPlacementMode.Bottom;
                    RootGrid.Children.Add(tip);
                    tip.IsOpen = true;
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
                    if (modes.Length > 0 && App.VM.Default.UseModes)
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
                    string logtext = "Compiling " + filetocompile.File.Name;
                    if (modes.Length > 0 && App.VM.Default.UseModes)
                        logtext += "; with modes: " + App.VM.Default.Modes;
                    if (App.VM.Default.AdditionalParameters.Trim().Length > 0 && App.VM.Default.UseParameters)
                        logtext += "; with parameters: " + App.VM.Default.AdditionalParameters;
                    App.VM.Log(logtext);
                    App.VM.Default.TexFileFolder = filetocompile.FileFolder;
                    App.VM.Default.TexFileName = filetocompile.FileName;
                    App.VM.Default.TexFilePath = filetocompile.File.Path;
                    //  ValueSet request = new ValueSet { { "compile", true } };
                    // AppServiceResponse response = await App.VM.AppServiceConnection.SendMessageAsync(request);
                    // display the response key/value pairs
                    //foreach (string key in response.Message.Keys)

                    // if ((string)response.Message[key] == "compiled")

                    bool compileSuccessful = false;

                    try
                    {
                        await Task.Run(async () => { await Compile(); });
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
                            App.VM.IsError = true;
                            StorageFile errorfile = error as StorageFile;
                            string text = await FileIO.ReadTextAsync(errorfile);
                            string newtext = text.Replace("  ", "").Replace("return", "").Replace("[\"", "\"").Replace("\"]", "\"").Replace(@"\n", "").Replace("=", ":");
                            var errormessage = JsonConvert.DeserializeObject<ConTeXtErrorMessage>(newtext);
                            App.VM.Log("Compiler error: " + errormessage.lasttexerror);
                            App.VM.ConTeXtErrorMessage = errormessage;
                        }
                        else
                        {
                            App.VM.IsPaused = true;
                            App.VM.IsError = false;
                            App.VM.IsVisible = false;
                        }

                        if (App.VM.Default.AutoOpenPDF && error == null)
                        {
                            StorageFile pdfout = await currFolder.TryGetItemAsync(Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".pdf") as StorageFile;
                            if (pdfout != null)
                            {
                                App.VM.Log("Opening " + System.IO.Path.GetFileNameWithoutExtension(App.VM.Default.TexFileName) + ".pdf");
                                if (App.VM.Default.InternalViewer)
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
                }
                catch (Exception f)
                {
                    App.VM.IsError = true;
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
            CompileTex(false, fi);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            VM.Save((sender as FrameworkElement).DataContext as FileItem);
        }

        private async Task<bool> InstallContext()
        {
            try
            {
                var storageFolder = ApplicationData.Current.LocalFolder;
                VM.Default.ContextDistributionPath = storageFolder.Path;
                StorageFile zipfile = await StorageFile.GetFileFromApplicationUriAsync(new System.Uri("ms-appx:///context-mswin.zip"));
                ZipFile.ExtractToDirectory(zipfile.Path, ApplicationData.Current.LocalFolder.Path, true);
                return true;
            }
            catch
            {
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

        public async Task<bool> Compile()
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
            p.OutputDataReceived += (e, f) => { //VM.Log(f.Data.);
            };
            //p.ErrorDataReceived += (e, f) => {Log(f.Data); };
            p.StartInfo = info;
            p.Start();
            p.BeginOutputReadLine();
            //using (StreamReader sr = p.StandardOutput)
            using (StreamWriter sw = p.StandardInput)
            {
                string param = "";
                if (VM.Default.Modes.Length > 0)
                    param = "--mode=" + VM.Default.Modes + " ";

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
            var fi = args.Tab.DataContext as FileItem;
            if (fi.IsChanged)
            {
                var save = new ContentDialog() { XamlRoot = XamlRoot, Title = "Do you want to save this file before closing?", PrimaryButtonText = "Yes", SecondaryButtonText = "No", DefaultButton = ContentDialogButton.Primary };

                if (await save.ShowAsync() == ContentDialogResult.Primary)
                {
                    await App.VM.Save(fi);
                }
            }

            VM.FileItems.Remove(fi);
            VM.CurrentProject.LastOpenedFiles = VM.FileItems.Select(x => x.FileName).ToList();
        }

        private void Pin_Click(object sender, RoutedEventArgs e)
        {
            var fi = (FileItem)(sender as FrameworkElement).DataContext;
            fi.IsPinned = !fi.IsPinned;
            int currind = VM.FileItems.IndexOf(fi);
            if (currind > 0)
                VM.FileItems.Move(currind, 0);
        }

        #endregion

        #region UI Updates

        public void ShowTeachingTip(string ID, object Target)
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
                VM.CurrentFileItem.CurrentLine = (int)(await (sender as CodeEditor).GetPositionAsync()).LineNumber;
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
                                    App.VM.GenerateTreeView(folder);

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
                                        //ZipFile.ExtractToDirectory(path + @"\" + project + ".zip", folder.Path,true);
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
                                        //var proj = new Project(folder.Name, folder, await App.VM.GenerateTreeView(folder, rootfile)) { RootFile = rootfile };
                                        StorageApplicationPermissions.FutureAccessList.AddOrReplace(folder.Name, folder, "");
                                        StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(folder.Name, folder, "");
                                        App.VM.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;

                                        App.VM.FileItemsTree.Clear();
                                        var proj = new Project(folder.Name, folder, App.VM.FileItemsTree);
                                        proj.RootFile = rootfile;
                                        App.VM.Default.ProjectList.Add(proj);
                                        App.VM.CurrentProject = proj;
                                        App.VM.GenerateTreeView(folder, rootfile);

                                        App.VM.Default.LastActiveProject = proj.Name;



                                        // App.AppViewModel.UpdateRecentAccessList();
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
                App.VM.Log(ex.Message);
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
                VM.InfoMessage(true,"Error on loading project",ex.Message, InfoBarSeverity.Error);
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

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog.ShowAsync();
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
                p.OutputDataReceived += (e, f) => {// VM.Log(f.Data);
                };
                p.StartInfo = info;
                p.Start();
                p.BeginOutputReadLine();


                //await Task.Delay(500);

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
            CompileTex();
        }

        private async void Btncompileroot_Click(object sender, RoutedEventArgs e)
        {
            await VM.SaveAll();
            CompileTex(true);
        }

        private async void Btnsave_Click(object sender, RoutedEventArgs e)
        {
            await VM.Save();
        }

        private async void btnsaveall_Click(object sender, RoutedEventArgs e)
        {
            await VM.SaveAll();
        }

        private void Modes_Click(object sender, RoutedEventArgs e)
        {
            ShowTeachingTip("Modes", sender);
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

        private void RemoveMode_Click(object sender, RoutedEventArgs e)
        {
            Mode m = (sender as FrameworkElement).DataContext as Mode;
            VM.CurrentProject.Modes.Remove(m);
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
                App.VM.Log(ex.Message);
            }
        }

        #endregion

        #region Ribbon : View

        private void ColorsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if ( e.ClickedItem is AccentColor accentColor)
            {
                SetColor(accentColor);
                VM.Default.AccentColor = accentColor.Name;
            }
        }

        private void SetColor(AccentColor accentColor)
        {
            ((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]).AccentColor = accentColor.Color;

            //ReloadPageTheme(this.RequestedTheme);
        }
        private void ReloadPageTheme(ElementTheme startTheme)
        {
            if (this.RequestedTheme == ElementTheme.Dark)
                VM.Default.Theme = "Light";
            else if (this.RequestedTheme == ElementTheme.Light)
                VM.Default.Theme = "Default";
            else if (this.RequestedTheme == ElementTheme.Default)
                VM.Default.Theme = "Dark";

            if (this.RequestedTheme != startTheme)
                ReloadPageTheme(startTheme);
        }

        private void ColorReset_Click(object sender, RoutedEventArgs e)
        {
            VM.AccentColor = VM.AccentColors.Find(x=>x.Color == (new Windows.UI.ViewManagement.UISettings()).GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent));
            SetColor(VM.AccentColor);
            VM.Default.AccentColor = "Default";
        }

        #endregion
    }
}
