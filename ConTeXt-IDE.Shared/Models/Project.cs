
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
                    App.VM.Default.SaveSettings();
                }
            }
        }

        public List<string> LastOpenedFiles { get => Get(new List<string>() { RootFile }); set => Set(value); }

        public ObservableCollection<Mode> Modes
        {
            get => Get(new ObservableCollection<Mode>() { new Mode() { Name = "print", IsSelected = false }, new Mode() { Name = "screen", IsSelected = false }, new Mode() { Name = "draft", IsSelected = false }, });
            set => Set(value);
        }
    }
}
