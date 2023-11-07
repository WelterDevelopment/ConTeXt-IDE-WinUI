
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
			if (App.VM?.Started == true)
			{
				App.VM?.Default?.SaveSettings();
			}
		}

		[JsonIgnore]
		public ObservableCollection<FileItem> Directory { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }

		[JsonIgnore]
		public StorageFolder Folder { get => Get<StorageFolder>(null); set { Set(value); if (value != null) Path = value.Path; } }

		public SyncTeX SyncTeX { get => Get<SyncTeX>(null); set => Set(value); }

		public string Name { get => Get(""); set => Set(value); }

		public string Path { get => Get(""); set => Set(value); }

		public string RootFilePath
		{
			get => Get<string>(null);
			set
			{
				Set(value);
				if (Directory != null && Directory.Count > 0)
				{
					//	Directory.FirstOrDefault().Children?.Where(x => x.FileName != value).ToList().ForEach(x => x.IsRoot = false);
					//	var df = Directory.Where(x => x.FileName == value);
					//	if (df.Count() == 1)
					//	{
					//		df.FirstOrDefault().IsRoot = true;
					//	}

					App.VM?.Log("Root file changed to " + System.IO.Path.GetFileName(value));

				}
			
			}
		}

		public List<string> LastOpenedFiles { get => Get(new List<string>() { RootFilePath }); set => Set(value); }

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

		
		public FileItem GetDirectoryByPath(FileItem folder, string itempath)
		{
			FileItem containingfolder = null;
			if (folder.File is StorageFolder)
			{
				if (folder.File.Path == itempath)
				{
					containingfolder = folder;
					return containingfolder;
				}
				else
				{
					foreach (FileItem fi in folder.Children)
					{
						if (fi.Type == FileItem.ExplorerItemType.Folder)
						{
							containingfolder = GetDirectoryByPath(fi, itempath);
							if (containingfolder!= null)
								return containingfolder;
						}
					}
				}
			}
			return containingfolder;
		}

		public FileItem GetFileItemByPath(FileItem folder, string filepath)
		{
			FileItem fileItem = null;
			foreach (FileItem fi in folder.Children)
			{
				if (fi.Type == FileItem.ExplorerItemType.Folder)
				{
					if (fi.File.Path == filepath)
					{
						fileItem=fi;
						return fileItem;
					}
					else
					{
						fileItem = GetFileItemByPath(fi, filepath);
						if (fileItem != null)
							return fileItem;
					}
				}
				else if (fi.Type == FileItem.ExplorerItemType.File)
				{
					if (fi.File.Path == filepath)
					{
						fileItem = fi;
						if (fileItem != null)
							return fileItem;
					}
				}
			}
			return fileItem;
		}

		public FileItem GetFolderByItem(FileItem root, FileItem target)
		{
			FileItem fileItem = null;
			if (root.Children.Contains(target))
				fileItem = root;
			else
			{
				foreach (FileItem fi in root.Children)
				{
					if (fi.Type == FileItem.ExplorerItemType.Folder)
						fileItem = GetFolderByItem(fi, target);
				}
			}
			return fileItem;
		}

		public FileItem RemoveFileItemByPath(FileItem folder, string itempath)
		{
			FileItem fileItem = null;
			bool isremoved = false;
			foreach (FileItem fi in folder.Children)
			{
				if (fi.Type == FileItem.ExplorerItemType.Folder)
				{
					if (fileItem == null)
					{
						if (fi.File.Path == itempath)
						{
							folder.Children.Remove(fi);
							fileItem = fi;
							isremoved = true;
							return fi;
						}
						else
							fileItem = RemoveFileItemByPath(fi, itempath);
					}
				}
				else if (fi.Type == FileItem.ExplorerItemType.File)
				{
					if (fi.File.Path == itempath)
					{
						folder.Children.Remove(fi);
						fileItem = fi;
						isremoved = true;
						return fi;
					}
				}
			}
			return fileItem;
		}

		public string AdditionalParameters { get => Get("--SomeParameter"); set => Set(value); }
		public bool UseAutoGenerate { get => Get(true); set => Set(value); }
		public bool UseNoConsole { get => Get(true); set => Set(value); }
		public bool UseNonStopMode { get => Get(true); set => Set(value); }
		public bool UseInterface { get => Get(false); set => Set(value); }
		public string Interface { get => Get("en"); set => Set(value); }
		public bool UseModes { get => Get(false); set => Set(value); }
		public bool UseEnvironments { get => Get(false); set => Set(value); }
		public bool UseSyncTeX { get => Get(true); set => Set(value); }
		public bool UseParameters { get => Get(false); set => Set(value); }
	}
}
