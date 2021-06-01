
using ConTeXt_IDE.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using Windows.Storage;

namespace ConTeXt_IDE.Models
{
    public class FileItem : Bindable
    {
        public FileItem(IStorageItem file, bool isRoot = false)
        {
            FileName = file != null ? file.Name : "";
            FileContent = LastSaveFileContent = "";
            IsRoot = isRoot;
            File = file;
            FileFolder = file != null ? Path.GetDirectoryName(file.Path) : "";
            if (file != null && file is StorageFile)
                FileLanguage = GetFileType(((StorageFile)file).FileType);
            IsLogFile = false;
        }

        public enum ExplorerItemType { Folder, File, ProjectRootFolder };

        public FileItem Parent { get => Get<FileItem>(null); set => Set(value); }

        public ObservableCollection<FileItem> Children { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }

        public ObservableCollection<OutlineItem> OutlineItems { get => Get(new ObservableCollection<OutlineItem>()); set => Set(value); }

        public int Level { get => Get(0); set => Set(value); }

        public IStorageItem File { get => Get<IStorageItem>(null); set => Set(value); }

        public string FileContent
        {
            get => Get("");
            set
            {
                Set(value);
                if (!string.IsNullOrEmpty(value))
                {
                    IsChanged = value != LastSaveFileContent;
                }
                // First idea for the Outline feature:
                //if (App.VM.Default.ShowOutline)
                //{
                //    //App.VM.CurrentEditor.FindMatchesAsync(@"(\\start(sub)*?(section|subject|part|chapter|title)\s*?\[\s*?)(title\s*?=\s*?\{?)(.*?)\}?\s*?([,\]])", false, true, false, null, true, 20);
                //    //var list = await editor.FindMatchesAsync(@"(\\start(sub)*?(section|subject|part|chapter|title)\s*?\[\s*?)(title\s*?=\s*?\{?)(.*?)\}?\s*?([,\]])", false, true, false, null, true, 20);
                //}
            }
        }

        public string FileFolder { get => Get(""); set => Set(value); }

        public string FileLanguage { get => Get(""); set => Set(value); }

        public string FileName { get => Get(""); set => Set(value); }

        public bool IsChanged { get => Get(false); set => Set(value); }

        public int CurrentLine { get => Get(1); set => Set(value); }

        public bool IsExpanded { get => Get(false); set => Set(value); }

        public bool IsLogFile { get => Get(false); set => Set(value); }

        public bool IsRoot { get => Get(false); set => Set(value); }

        public bool IsPinned { get => Get(false); set => Set(value); }

        public bool IsSelected { get => Get(false); set => Set(value); }

        public string LastSaveFileContent { get => Get(""); set => Set(value); }

        public ExplorerItemType Type { get => Get(ExplorerItemType.File); set => Set(value); }


        public static string GetFileType(string ext)
        {
            switch (ext)
            {
                case ".tex": return "context";
                case ".mkiv": return "context";
                case ".mkii": return "context";
                case ".mkxl": return "context";
                case ".mkvi": return "context";
                case ".lua": return "lua";
                case ".json": return "javascript";
                case ".js": return "javascript";
                case ".r": return "r";
                case ".py": return "python";
                case ".md": return "markdown";
                case ".html": return "html";
                case ".xml": return "xml";
                case ".yaml": return "yaml";
                case ".ts": return "typescript";
                case ".log": return "log";
                case ".png": return "bitmap";
                case ".bmp": return "bitmap";
                case ".svg": return "vector";
                default:
                    return "misc";
            }
        }
    }
}
