using CodeEditorControl_WinUI;
using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI;

namespace ConTeXt_IDE.ViewModels
{
    public class ViewModel : Helpers.Bindable
    {
        public ObservableCollection<FileActivatedEventArgs> FileActivatedEvents = new ObservableCollection<FileActivatedEventArgs>() { };
        public ObservableCollection<FileItem> FileItemsTree = new ObservableCollection<FileItem>();

        private string rootFile;

        public ViewModel()
        {
            try
            {
                // Default = Settings.Default;
                FileItems = new ObservableCollection<FileItem>();
                CurrentFileItem = FileItems.Count > 0 ? FileItems.FirstOrDefault() : new FileItem(null);

                FileItems.CollectionChanged += FileItems_CollectionChanged1;


                if (Default.AccentColor == "Default")
                    AccentColor = AccentColors.Find(x => x.Color == (new Windows.UI.ViewManagement.UISettings()).GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent));
                else
                    AccentColor = AccentColors.Find(x => x.Name == Default.AccentColor);

                ((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]).AccentColor = AccentColor.Color;
                //Application.Current.Resources["SystemAccentColor"] = AccentColor.Color;
               // ColorPaletteResources cpr = FindColorPaletteResourcesForTheme("Light");
                //cpr.Accent = AccentColor.Color;

                //var ce = new CodeEditor();
                //EditorOptions = ce.Options;
            

            }
            catch (TypeInitializationException ex)
            {
                Log("TypeInitializationException" + ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                Log("Exception" + ex.Message);
            }
        }

        private void EditorOptions_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
           
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

        

        public AppServiceConnection AppServiceConnection { get; set; }

        public BackgroundTaskDeferral AppServiceDeferral { get; set; }

        public string Blocks { get => Get<string>(); set => Set(value); }

        public bool InfoOpen { get => Get(false); set => Set(value); }
        public string InfoTitle { get => Get(""); set => Set(value); }
        public string InfoText { get => Get(""); set => Set(value); }
        public InfoBarSeverity InfoSeverity { get => Get(InfoBarSeverity.Informational); set => Set(value); }

        public void InfoMessage(bool infoOpen, string infoTitle = "", string infoText = "", InfoBarSeverity infoSeverity = InfoBarSeverity.Informational)
        {
            InfoOpen = infoOpen;
            InfoTitle = infoTitle;
            InfoText = infoText;
            InfoSeverity = infoSeverity;
            Log(infoTitle + ": " + infoText);
        }

        public ConTeXtErrorMessage ConTeXtErrorMessage { get => Get(new ConTeXtErrorMessage()); set => Set(value); }

        public FileItem CurrentFileItem { get => Get(new FileItem(null)); set { Set(value);  } }
        public FileItem CurrentRootItem { get => Get<FileItem>(null); set => Set(value); }

        public Project CurrentProject { get => Get(new Project()); set { Set(value); IsProjectLoaded = value?.Folder != null; if (value?.Folder != null) Log("Project " + value.Name + " loaded."); } }

        public Settings Default { get; } = Settings.Default;

       



        public ObservableCollection<FileItem> FileItems { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }

        public ObservableCollection<Helpfile> HelpFiles { get; } = new ObservableCollection<Helpfile>() {
            new Helpfile() { FriendlyName = "Manual", FileName = "ma-cb-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\manuals\" },
            new Helpfile() { FriendlyName = "Commands", FileName = "setup-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\qrcs\" },
        };

        

        private static Color ColorFromHex(string hex)
        {
            string colorStr = hex;
            colorStr = colorStr.Replace("#", string.Empty);
            var r = (byte)System.Convert.ToUInt32(colorStr.Substring(0, 2), 16);
            var g = (byte)System.Convert.ToUInt32(colorStr.Substring(2, 2), 16);
            var b = (byte)System.Convert.ToUInt32(colorStr.Substring(4, 2), 16);

           return Color.FromArgb(255, r, g, b);
        }

        public List<AccentColor> AccentColors { get; } = new List<AccentColor>() { 
           // new AccentColor("System", (new Windows.UI.ViewManagement.UISettings()).GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent)), 
            new AccentColor("Yellow Gold", ColorFromHex( "FFB900")),
            new AccentColor("Gold", ColorFromHex("FF8C00")),
            new AccentColor("Orange Bright", ColorFromHex("F7630C")),
            new AccentColor("Orange Dark", ColorFromHex("CA5010")),
            new AccentColor("Rust", ColorFromHex("DA3B01")),
            new AccentColor("Pale Rust", ColorFromHex("EF6950")),
            new AccentColor("Brick Red", ColorFromHex("D13438")),
            new AccentColor("Mod Red", ColorFromHex("FF4343")),

            new AccentColor("Pale Red", ColorFromHex("E74856")),
            new AccentColor("Red", ColorFromHex("E81123")),
            new AccentColor("Rose Bright", ColorFromHex("EA005E")),
            new AccentColor("Rose", ColorFromHex("C30052")),
            new AccentColor("Plum Light", ColorFromHex("E3008C")),
            new AccentColor("Plum", ColorFromHex("BF0077")),
            new AccentColor("Orchid Light", ColorFromHex("C239B3")),
            new AccentColor("Orchid", ColorFromHex("9A0089")),

            new AccentColor("Default Blue", ColorFromHex("0078D7")),
            new AccentColor("Navy Blue", ColorFromHex("0063B1")),
            new AccentColor("Purple Shadow", ColorFromHex("8E8CD8")),
            new AccentColor("Purple Shadow Dark", ColorFromHex("6B69D6")),
            new AccentColor("Iris Pastel", ColorFromHex("8764B8")),
            new AccentColor("Iris Spring", ColorFromHex("744DA9")),
            new AccentColor("Violet Red Light", ColorFromHex("B146C2")),
            new AccentColor("Violet Red", ColorFromHex("881798")),

            new AccentColor("Cool Blue Bright", ColorFromHex("0099BC")),
            new AccentColor("Cool Blue", ColorFromHex("2D7D9A")),
            new AccentColor("Seafoam", ColorFromHex("00B7C3")),
            new AccentColor("Seafoam Team", ColorFromHex("038387")),
            new AccentColor("Mint Light", ColorFromHex("00B294")),
            new AccentColor("Mint Dark", ColorFromHex("018574")),
            new AccentColor("Turf Green", ColorFromHex("00CC6A")),
            new AccentColor("Sport Green", ColorFromHex("10893E")),

            new AccentColor("Gray", ColorFromHex("7A7574")),
            new AccentColor("Gray Brown", ColorFromHex("5D5A58")),
            new AccentColor("Steel Blue", ColorFromHex("68768A")),
            new AccentColor("Metal Blue", ColorFromHex("515C6B")),
            new AccentColor("Pale Moss", ColorFromHex("567C73")),
            new AccentColor("Moss", ColorFromHex("486860")),
            new AccentColor("Meadow Green", ColorFromHex("498205")),
            new AccentColor("Green", ColorFromHex("107C10")),

            new AccentColor("Overcast", ColorFromHex("767676")),
            new AccentColor("Storm", ColorFromHex("4C4A48")),
            new AccentColor("Blue Gray", ColorFromHex("69797E")),
            new AccentColor("Gray Dark", ColorFromHex("4A5459")),
            new AccentColor("Liddy Green", ColorFromHex("647C64")),
            new AccentColor("Sage", ColorFromHex("525E54")),
            new AccentColor("Camouflage Desert", ColorFromHex("847545")),
            new AccentColor("Camouflage", ColorFromHex("7E735F")),
        };

        public AccentColor AccentColor { get => Get<AccentColor>(); set { Set(value); } }

        public bool IsError { get => Get(false); set => Set(value); }

        public bool IsIndeterminate { get => Get(true); set => Set(value); }

        public int ProgressValue { get => Get(0); set => Set(value); }

        public bool IsFileItemLoaded { get => Get(false); set { Set(value); if (value) { IsVisible = false; } else { IsVisible = false; } } }

        public bool IsInternalViewerActive { get => Get(false); set => Set(value); }

        public bool IsPaused { get => Get(false); set { Set(value); } }

        public bool Started { get => Get(false); set { Set(value); } }

        public bool IsInstalled { get => Get(false); set { Set(value); } }

        public bool IsProjectLoaded { get => Get(false); set => Set(value); }

        public bool IsSaving { get => Get(false); set { Set(value); if (value) { IsVisible = true; IsPaused = false; } if (!value && !IsError) { IsVisible = false; } } }

        public bool IsVisible { get => Get(false); set => Set(value); }

        public string NVHead { get => Get(""); set => Set(value); }

        public Command SelectedCommand { get => Get<Command>(null); set { if (SelectedCommand != null) { SelectedCommand.IsSelected = false; SelectedCommand.SelectedIndex = -1; } Set(value); if (value != null) value.IsSelected = true; } }

        public List<string> ContextCommandGroupList { get => Get(new List<string>()); set => Set(value); }

        public CollectionViewSource cvs { get => Get(new CollectionViewSource() { IsSourceGrouped = true }); set => Set(value); }

        public ObservableCollection<ContextCommand> ContextCommands { get => Get(new ObservableCollection<ContextCommand>()); set => Set(value); }

        

        public StorageItemMostRecentlyUsedList RecentAccessList { get => Get<StorageItemMostRecentlyUsedList>(); set => Set(value); }

        public string SelectedPath { get => Get(""); set => Set(value); }

        

        public async Task GenerateTreeView(StorageFolder folder, string rootfile = null)
        {
            rootFile = rootfile;

            if (folder != null)
            {
                FileItemsTree.Add(new FileItem(folder) { IsExpanded = true, Type = FileItem.ExplorerItemType.ProjectRootFolder });
                await DirWalk(folder);
            }
            else
            {
                Log("Operation cancelled.");
            }
        }
        private readonly List<string> cancelWords = new List<string> { ".gitignore", ".tuc", ".log", ".pgf" };
        private async Task DirWalk(StorageFolder sDir, FileItem currFolder = null, int level = 0)
        {
            try
            {
                var folders = await sDir.GetFoldersAsync();
                var files = await sDir.GetFilesAsync();
                foreach (StorageFolder d in folders)
                {
                    if (!d.Name.StartsWith("."))
                    {
                        var SubFolder = new FileItem(d) { Type = FileItem.ExplorerItemType.Folder, FileName = d.Name, IsRoot = false, Level = level };
                        if (level > 0)
                            currFolder.Children.Add(SubFolder);
                        else
                            FileItemsTree[0].Children.Add(SubFolder);
                        await DirWalk(d, SubFolder, level + 1);
                    }
                }
                foreach (StorageFile f in files)
                {
                    string filename = f.Name;
                    bool iscompiledpdf = (filename.StartsWith("project_") | filename.StartsWith("prd_") | filename.StartsWith("c_") | filename.StartsWith("env_") | filename.StartsWith("p-") | filename.StartsWith("t-")) && filename.EndsWith(".pdf");
                    if (!filename.StartsWith(".") && !cancelWords.Contains(f.FileType) && !iscompiledpdf)
                    {
                        var fi = new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = filename, IsRoot = false, Level = level };
                        if (level > 0)
                            currFolder.Children.Add(fi);
                        else
                        {
                            fi.IsRoot = fi.FileName == rootFile;
                            if (fi.IsRoot) 
                                CurrentRootItem = fi;
                            FileItemsTree[0].Children.Add(fi);
                        }
                    }
                }
            }
            catch (Exception excpt)
            {
                Log("Error in generating the project directory tree: " + excpt.Message);
            }
        }

        public FileItem InitializeFileItem(StorageFile File, string Content = "", bool IsRoot = true)
        {
            return new FileItem(File, IsRoot) { FileContent = Content };
        }

        public async Task<bool> Log(object log, string title = "Error:")
        {
            try
            {
                Blocks = log.ToString();
                await Task.Delay(100);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Dictionary<string, string> Meta(string m)
        {
            return m.Split(';').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
        }

        public async void OpenFile(FileItem File, bool ProjectLoad = false)
        {
            try
            {
                if (!FileItems.Contains(File))
                {
                    var openfile = FileItems.Where(x => x.File.Path == File.File.Path);

                    var read = await FileIO.ReadTextAsync((StorageFile)File.File);
                    File.LastSaveFileContent = read;
                    //await Task.Delay(500);

                    File.FileContent = read;

                    if (openfile.Count() > 0)
                    {
                        if (CurrentFileItem == openfile.FirstOrDefault())
                        {
                            FileItems[FileItems.IndexOf(openfile.FirstOrDefault())] = File;
                            //CurrentFileItem = File;
                        }
                        else
                        {
                            FileItems[FileItems.IndexOf(openfile.FirstOrDefault())] = File;
                            //CurrentFileItem = File;
                        }
                    }
                    else
                    {
                        FileItems.Add(File);
                        //await Task.Delay(500); // ObservableCollection.Add() is slow, so setting CurrentFileItem = File will result in an exception because FileItems is still empty. A cleaner approach is needed. 
                        await Log("File " + File.FileName + " opened");
                    }

                    if (FileItems.Contains(File))
                        CurrentFileItem = File;

                    //if (!ProjectLoad)
                    //    CurrentProject.LastOpenedFiles = FileItems.Select(x => x.FileName).ToList();
                }
                else
                {
                    CurrentFileItem = File;
                }
            }
            catch (Exception ex)
            {
                await Log("Cannot open selected file: " + ex.Message);
            }
        }

        public async Task LoadProject(Project proj)
        {
            try
            {
                FileItems?.Clear();
                var f = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(proj.Name);

                var list = Default.ProjectList.Where(x => x.Name == f.Name);

                if (list.Count() == 1)
                {
                    var project = list.FirstOrDefault();

                    project.Folder = f;
                    FileItemsTree = new ObservableCollection<FileItem>();
                   await  GenerateTreeView(f, proj.RootFile);

                    project.Directory = FileItemsTree;

                    CurrentProject = project;
                }

                Default.LastActiveProject = proj.Name;

                if (!string.IsNullOrEmpty(CurrentProject?.RootFile))
                {
                    //await Task.Delay(1000);
                    if (!Default.StartWithLastOpenFiles && CurrentRootItem != null)
                        OpenFile(CurrentRootItem, true);
                    else if (Default.StartWithLastOpenFiles)
                    {
                        foreach (var item in CurrentProject.LastOpenedFiles)
                        {
                            FileItem fileItem = CurrentProject.Directory[0]?.Children?.FirstOrDefault(x => x.FileName == item);
                            if (fileItem != null) OpenFile(fileItem, true);
                        }
                    }
                   // await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                InfoMessage(true, "Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        public async Task Startup()
        {
            try
            {
                //HelpItems = PopulateHelpItems();
                // UpdateRecentAccessList();
                if (Default.StartWithLastActiveProject && !string.IsNullOrWhiteSpace(Default.LastActiveProject))
                {
                    RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
                    if (RecentAccessList.ContainsItem(Default.LastActiveProject))
                    {
                        IsSaving = true;
                        var folder = await RecentAccessList.GetFolderAsync(Default.LastActiveProject);
                        //var f = RecentAccessList.Entries.Where(x => x.Token == folder.Name).FirstOrDefault();
                        var list = Default.ProjectList.Where(x => x.Name == folder.Name);
                        if (list != null && list.Count() == 1)
                        {
                            var project = list.FirstOrDefault();
                            await LoadProject(project);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Log("Error on ViewModel startup: " + ex.Message);
            }
            finally
            {
                IsSaving = false;
                Started = true;
            }
        }

        public async Task Save(FileItem fileItem = null)
        {
            FileItem filetosave = fileItem ?? CurrentFileItem;

            if (filetosave != null)
                if (!IsSaving && filetosave.File != null)
                {
                    try
                    {
                        if (filetosave.IsChanged)
                        {
                            IsSaving = true;
                            IsPaused = false;
                            string cont = filetosave.FileContent ?? " ";
                            var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(cont, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                            await FileIO.WriteBufferAsync(CurrentFileItem.File as StorageFile, buffer);
                            Default.TexFileName = filetosave.FileName;
                            Default.TexFilePath = filetosave.File.Path;
                            Default.TexFileFolder = filetosave.FileFolder;
                            filetosave.LastSaveFileContent = filetosave.FileContent;
                            filetosave.IsChanged = false;
                            IsSaving = false;
                            IsPaused = true;
                            IsVisible = false;
                            Log("File " + filetosave.File.Name + " saved");
                        }
                    }
                    catch (Exception ex)
                    {
                        IsError = true;
                        IsSaving = false;
                        Log("Error on Saving file: " + ex.Message);
                    }
                }
                else Log("Cannot save this file");
        }

        public async Task SaveAll()
        {
            try
            {
                if (!IsSaving && CurrentFileItem != null)
                {
                    if (CurrentFileItem.File != null)
                    {
                        IsSaving = true;
                        IsPaused = false;
                        foreach (var item in FileItems)
                        {
                            if (item.IsChanged)
                            {
                                string cont = item.FileContent ?? " ";
                                var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(cont, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                                await FileIO.WriteBufferAsync((StorageFile)item.File, buffer);
                                item.LastSaveFileContent = item.FileContent;
                                item.IsChanged = false;
                                Log("File " + item.File.Name + " saved");
                            }
                        }
                        Default.TexFileName = CurrentFileItem.FileName;
                        Default.TexFilePath = CurrentFileItem.File.Path;
                        Default.TexFileFolder = CurrentFileItem.FileFolder;
                        IsSaving = false;
                        IsPaused = true;
                        IsVisible = false;
                    }
                }
                //else LOG("Error");
            }
            catch (Exception ex)
            {
                IsError = true;
                IsSaving = false;
                Log("Error on Saving file: " + ex.Message);
            }
        }

        private async void DirSearch(StorageFolder sDir, int level = 0)
        {
            try
            {
                foreach (StorageFolder d in await sDir.GetFoldersAsync())
                {
                    if (!d.Name.StartsWith("."))
                    {
                        FileItem SubFolder = new FileItem(d) { Type = FileItem.ExplorerItemType.Folder, FileName = d.Name, IsRoot = false };
                        foreach (StorageFile f in await d.GetFilesAsync())
                        {
                            if (!cancelWords.Contains(f.FileType))
                            {
                                SubFolder.Children.Add(new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false });
                            }
                        }
                        FileItemsTree.Add(SubFolder);
                        DirSearch(d, level + 1);
                    }
                }
                if (level == 0)
                {
                    foreach (StorageFile f in await sDir.GetFilesAsync())
                    {
                        if (!cancelWords.Contains(f.FileType))
                        {
                            FileItemsTree.Add(new FileItem(f) { File = f, Type = FileItem.ExplorerItemType.File, FileName = f.Name, IsRoot = false });
                        }
                    }
                }
            }
            catch (Exception excpt)
            {
                Log("Error in generating the directory tree: " + excpt.Message);
            }
        }

        private async void FileItems_CollectionChanged1(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    await Task.Delay(500);

            //    CurrentFileItem = e.NewItems[0] as FileItem;
            //}
            if (FileItems?.Count == 0)
            {
                IsFileItemLoaded = false;
            }
            else
            {
                IsFileItemLoaded = true;
            }
        }
    }
}