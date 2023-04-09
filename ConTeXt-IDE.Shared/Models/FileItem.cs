
using CodeEditorControl_WinUI;
using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Shared.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace ConTeXt_IDE.Models
{
	public class FileItemComparer : IComparer<FileItem>
	{
		public int Compare(FileItem x, FileItem y)
		{
			return x.FileName.CompareTo(y.FileName);
		}
	}


	public class FileItem : Helpers.Bindable, IComparable<FileItem>
	{
		public FileItem(IStorageItem file, bool isRoot = false)
		{
			FileName = file != null ? file.Name : "";
			FileContent = LastSaveFileContent = "";
			IsRoot = isRoot;
			File = file;
			FileFolder = file != null ? Path.GetDirectoryName(file.Path) : "";
			if (file != null)
			{
				if (file is StorageFile)
				{
					FileLanguage = GetFileType(((StorageFile)file).FileType);
					Type = ExplorerItemType.File;
				}
				else if (file is StorageFolder)
				{
					Type = ExplorerItemType.Folder;
				}
			}
			IsLogFile = false;

		}

		public enum ExplorerItemType { Folder, File, ProjectRootFolder };

		public FileItem Parent { get => Get<FileItem>(null); set => Set(value); }

		public ObservableCollection<FileItem> Children { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }



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

			}
		}

		public string FileFolder { get => Get(""); set => Set(value); }

		public string FileLanguage { get => Get("Text"); set { Set(value); Language = FileLanguages.LanguageList.FirstOrDefault(x => x.Name == value); IsTexFile = value == "ConTeXt"; } }

		public Language Language { get => Get<Language>(); set => Set(value); }

		public string FileName { get => Get(""); set => Set(value); }

		public bool IsChanged { get => Get(false); set => Set(value); }

		public bool IsTexFile { get => Get(false); set => Set(value); }

		public Place CurrentLine
		{
			get => Get(new Place(0, 0));
			set
			{
				if (CurrentLine.iLine != value.iLine)
				{
					if (App.VM?.OutlineItems?.Count > 0)
					{
						App.VM.SelectedOutlineItem = App.VM?.OutlineItems.Where(x => value.iLine + 1 >= x.Row).LastOrDefault();

					}
				}
				Set(value);
			}
		}

		public ObservableCollection<EditAction> EditActionHistory { get => Get(new ObservableCollection<EditAction>()); set => Set(value); }

		public bool IsExpanded { get => Get(false); set => Set(value); }

		public bool IsLogFile { get => Get(false); set => Set(value); }

		public bool IsRoot { get => Get(false); set => Set(value); }

		public bool IsPinned { get => Get(false); set => Set(value); }

		public bool IsSelected { get => Get(false); set => Set(value); }

		public bool IsOpened { get => Get(false); set => Set(value); }

		public string LastSaveFileContent { get => Get(""); set => Set(value); }

		public ExplorerItemType Type { get => Get(ExplorerItemType.File); set => Set(value); }




		public static string GetFileType(string ext)
		{
			switch (ext)
			{
				case ".tex": return "ConTeXt";
				case ".mkiv": return "ConTeXt";
				case ".mkii": return "ConTeXt";
				case ".mkxl": return "ConTeXt";
				case ".mkvi": return "ConTeXt";
				case ".lua": return "Lua";
				case ".tuc": return "Lua";
				case ".json": return "Text";
				case ".js": return "Text";
				case ".r": return "Text";
				case ".py": return "Text";
				case ".md": return "Markdown";
				case ".htm": return "Xml";
				case ".html": return "Xml";
				case ".xml": return "Xml";
				case ".yaml": return "Xml";
				case ".ts": return "Text";
				case ".log": return "Log";
				case ".png": return "bitmap";
				case ".bmp": return "bitmap";
				case ".svg": return "vector";
				case ".txt": return "vector";
				case ".csv": return "Text";
				default:
					return "Text";
			}
		}

		public int CompareTo(FileItem other)
		{
			return FileName.CompareTo(other.FileName);
		}

		public override string ToString()
		{
			return FileName;
		}

	}
}
