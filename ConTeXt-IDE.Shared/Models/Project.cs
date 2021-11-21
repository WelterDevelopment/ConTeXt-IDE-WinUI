
using ConTeXt_IDE.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace ConTeXt_IDE.Models
{
    public class Project : Bindable
    {
        public Project(string name = "", StorageFolder folder = null, ObservableCollection<FileItem> directory = null)
        {
            Directory = directory;
            Name = name;
            Folder = folder;
            PropertyChanged += Project_PropertyChanged;
        }

        private void Project_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            App.VM?.Default?.SaveSettings();
        }

        [JsonIgnore]
        public ObservableCollection<FileItem> Directory { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }

        [JsonIgnore]
        public StorageFolder Folder { get => Get<StorageFolder>(null); set => Set(value); }

        public string Name { get => Get(""); set => Set(value); }

        public string RootFile
        {
            get => Get("");
            set
            {
                Set(value);
                if (Directory != null && Directory.Count > 0)
                {
                    Directory.FirstOrDefault().Children?.Where(x => x.FileName != value).ToList().ForEach(x => x.IsRoot = false);
                    var df = Directory.Where(x => x.FileName == value);
                    if (df.Count() == 1)
                    {
                        df.FirstOrDefault().IsRoot = true;
                    }

                    App.VM.Log("Root file changed to " + RootFile);
                    
                }
            }
        }

        public List<string> LastOpenedFiles { get => Get(new List<string>() { RootFile }); set => Set(value); }

        public ObservableCollection<Mode> Modes
        {
            get => Get(new ObservableCollection<Mode>() { new Mode() { Name = "print", IsSelected = false }, new Mode() { Name = "screen", IsSelected = false }, new Mode() { Name = "draft", IsSelected = false }, });
            set => Set(value);
        }

        public ObservableCollection<Mode> Environments
        {
            get => Get(new ObservableCollection<Mode>() { new Mode() { Name = "env_thesis", IsSelected = false }, });
            set => Set(value);
        }

        public FileItem GetFileItemByName(FileItem folder, string filename)
        {
            FileItem fileItem = null;
            foreach (FileItem fi in folder.Children)
            {
                if (fi.Type == FileItem.ExplorerItemType.Folder)
                {
                    if (fileItem == null)
                        fileItem = GetFileItemByName(fi, filename);
                }
                else if (fi.Type == FileItem.ExplorerItemType.File)
                {
                    if (fi.FileName == filename)
                    {
                        fileItem = fi;
                    }
                }
            }
            return fileItem;
        }

        public string AdditionalParameters { get => Get(""); set => Set(value); }
        public bool UseAutoGenerate { get => Get(true); set => Set(value); }
        public bool UseNoConsole { get => Get(true); set => Set(value); }
        public bool UseNonStopMode { get => Get(true); set => Set(value); }
        public bool UseInterface { get => Get(false); set => Set(value); }
        public string Interface { get => Get("en"); set => Set(value); }
        public bool UseModes { get => Get(false); set => Set(value); }
        public bool UseEnvironments { get => Get(false); set => Set(value); }
        public bool UseParameters { get => Get(false); set => Set(value); }
    }
}
