using CodeEditorControl_WinUI;
using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using ConTeXt_IDE.Shared.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
using System.Text;
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
using Windows.UI.StartScreen;

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
				MenuSyncTeX.Command = new RelayCommand(() => { FindInPDF(); });
				MenuSave.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control });
				MenuCompile.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Enter, Modifiers = VirtualKeyModifiers.Control });
				MenuSyncTeX.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Space, Modifiers = VirtualKeyModifiers.Control });
				Binding myBinding = new Binding();
				myBinding.Path = new("CurrentFileItem.IsTexFile");
				MenuCompile.SetBinding(MenuFlyoutItem.IsEnabledProperty, myBinding);
			}
			catch (Exception ex)
			{
				App.VM?.InfoMessage("Exception", ex.Message, InfoBarSeverity.Error);
			}
		}

		private async void FindInPDF()
		{
			try
			{
				if (VM.CurrentProject.SyncTeX != null)
				{
					int line = VM.CurrentFileItem.CurrentLine.iLine + 1;
					string file = VM.CurrentFileItem.FileName;
					SyncTeX syncTeX = VM.CurrentProject.SyncTeX;
					SyncTeXInputFile input = syncTeX.SyncTeXInputFiles.FirstOrDefault(x => x.Name.Contains(file));
					if (input != null)
					{
						int id = syncTeX.SyncTeXInputFiles.FirstOrDefault(x => x.Name.Contains(file)).Id;
						var entries = syncTeX.SyncTeXEntries.Where(x => x.Id == id && x.Line == line);
						if (entries.Count() > 0)
						{
							SyncTeXEntry entry = entries.First();
							//VM.Page = entry.Page;
							await PDFReader.ScrollToPosition(entry.Page, entry.YOffset, entry.Depth);
						}
						else
						{
							VM.Log("The position you selected has no SyncTeX entry. Please try again with a line that contains plain text.");
						}
					}
					else
					{
						VM.Log($"The file {file} has no SyncTeX entry. Please try again with a file that contains plain text.");
					}
				}
				else
				{
					VM.InfoMessage("SyncTeX Warning", "To use SyncTeX, please compile the root file with the --synctex parameter enabled.");
				}
			}
			catch (Exception ex)
			{
				VM.Log("Error on parsing the .synctex file: " + ex.Message);
			}
		}

		#region Page Load

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			SetColor(VM.AccentColor, (ElementTheme)Enum.Parse(typeof(ElementTheme), VM.Default.Theme), false);
		}

		public async void FirstStart()
		{
			if (VM.Default.FirstStart)
			{
				//ShowTeachingTip("AddProject", Btnaddproject);
				VM.Default.FirstStart = false;
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
		private MenuFlyoutItem MenuSyncTeX = new MenuFlyoutItem() { Text = "Find in PDF", Icon = new SymbolIcon() { Symbol = Symbol.Find } };

		private async void Codewriter_Loaded(object sender, RoutedEventArgs e)
		{
			CodeWriter cw = sender as CodeWriter;

			VM.Codewriter = cw;

			if (!cw.ContextMenu.Items.Any(x => { if (x is MenuFlyoutItem item) return item.Text == "Save"; else return false; }))
				cw.Action_Add(MenuSave);

			if (!cw.ContextMenu.Items.Any(x => { if (x is MenuFlyoutItem item) return item.Text == "Compile"; else return false; }))
				cw.Action_Add(MenuCompile);

			if (!cw.ContextMenu.Items.Any(x => { if (x is MenuFlyoutItem item) return item.Text == "Find in PDF"; else return false; }) && VM.CurrentProject.UseSyncTeX)
				cw.Action_Add(MenuSyncTeX);

			if (VM.CurrentFileItem.IsTexFile)
				cw.UpdateSuggestions();

			cw.ScrollToLine(VM.CurrentFileItem.CurrentLine.iLine);
			cw.Focus(FocusState.Keyboard);


			cw.Lines.CollectionChanged += Lines_CollectionChanged;
			cw.DoubleClicked += Cw_DoubleClicked;

		}

		// SyncTex logic
		private void Cw_DoubleClicked(object sender, EventArgs e)
		{
			//CodeWriter cw = sender as CodeWriter;
			//PDFReader.PDFjsViewerWebView.NavigateToString(cw.SelectedText);
		}

		private void Lines_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			//if (VM.Codewriter.IsInitialized && VM.CurrentFileItem.FileLanguage == "ConTeXt")
			//{
			//    VM.UpdateOutline((sender as ObservableCollection<Line>).ToList(), true);
			//}
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
				VM.InfoMessage("Installing", "Extracting the ConTeXt Distribution to the app's package Folder", InfoBarSeverity.Informational);
				await Task.Run(async () => { InstallSuccessful = await InstallContext(); });
				//InstallSuccessful = await InstallContext();
				if (InstallSuccessful)
				{
					VM.Default.DistributionInstalled = true;
					VM.IsSaving = false;
					VM.IsPaused = true;
					VM.IsVisible = false;
					VM.InfoMessage("Installation complete!", "Your ConTeXt distribution is up n' running!", InfoBarSeverity.Success);
				}
				else
				{
					VM.InfoMessage("ConTeXt Installation failed", "Please try again after reinstalling the app. Feel free to contact the app's author in case the problem persists!", InfoBarSeverity.Error);
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
				VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
			}
		}

		private async void PDFReader_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				//PDFReader.PDFjsViewerWebView.WebMessageReceived += (a,b) => { VM.Log(b.TryGetWebMessageAsString()); };

			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
		}

		bool opening = true;
		private void PDFReader_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
		{
			sender.NavigationStarting += (a, b) => { if (!opening) b.Cancel = true; };
			(sender as WebView2).CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			(sender as WebView2).CoreWebView2.Settings.AreDevToolsEnabled = true;
			(sender as WebView2).CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
			(sender as WebView2).CoreWebView2.Settings.IsZoomControlEnabled = true;
			(sender as WebView2).CoreWebView2.Settings.IsScriptEnabled = true;
			(sender as WebView2).CoreWebView2.Settings.IsWebMessageEnabled = true;
			(sender as WebView2).CoreWebView2.Settings.AreHostObjectsAllowed = true;
			//(sender as WebView2).CoreWebView2.WebMessageReceived += (a,b) => { VM.Log(b.Source + " : " + b.WebMessageAsJson); };
			//(sender as WebView2).CoreWebView2.WebResourceRequested += (a, b) => { VM.Log(b.Response.Content); };
			//(sender as WebView2).CoreWebView2.WebResourceResponseReceived += (a, b) => { VM.Log(b.Response.ToString()); };
		}

		#endregion

		#region Page Close

		public async Task<bool> MainPage_CloseRequested()
		{
			bool unsaved = VM.FileItems.Any(x => x.IsChanged);
			if (unsaved)
			{
				var save = new ContentDialog() { XamlRoot = XamlRoot, CloseButtonText = "Cancel", Title = "Do you want to save the open unsaved files before closing?", PrimaryButtonText = "Yes", SecondaryButtonText = "No", DefaultButton = ContentDialogButton.Primary };
				var result = await save.ShowAsync();
				if (result == ContentDialogResult.Primary)
				{
					await VM.SaveAll();
				}
				return result == ContentDialogResult.None;
			}
			return false;
		}

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
						VM.InfoMessage("Error", "The file" + name + " does already exist.", InfoBarSeverity.Error);
				}
			}
			catch (Exception ex)
			{
				VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
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
				var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Set folder name", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
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
						VM.InfoMessage("Error", "The folder" + name + " does already exist.", InfoBarSeverity.Error);
					}
				}
			}
			catch (Exception ex)
			{
				VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
			}
		}

		private async void AddFileRoot_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string name = "file.tex";
				var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Set file name", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };

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
				var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Set folder name", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
				TextBox tb = new TextBox() { Text = name };
				cd.Content = tb;
				var result = await cd.ShowAsync();
				if (result == ContentDialogResult.Primary)
				{
					name = tb.Text;
					var folder = VM.CurrentProject.Folder;
					if (await folder.TryGetItemAsync(name) == null)
					{
						var subfolder = await folder.CreateFolderAsync(name);
						var fi = new FileItem(subfolder) { Type = FileItem.ExplorerItemType.Folder };
						VM.CurrentProject.Directory[0].Children.Insert(0, fi);
					}
					else
						VM.Log(name + " does already exist.");
				}
			}
			catch (Exception ex)
			{
				VM.Log("Exception on adding Folder: " + ex.Message);
			}
		}

		private async void OpeninExplorer_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				await Launcher.LaunchFolderAsync(VM.CurrentProject.Folder);
			}
			catch (Exception ex)
			{
				VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
			}
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var fi = (FileItem)(sender as FrameworkElement).DataContext;
				RemoveItem(VM.CurrentProject.Directory, fi);
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
				VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
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
				VM.Log($"Renaming {type.ToString().ToLower()} {startstring} to {tb.Text}");
				return tb.Text;
			}
			else
				return startstring;
		}

		private void EditorTabViewDrag(object sender, DragEventArgs e)
		{
			try
			{
				e.AcceptedOperation = DataPackageOperation.Link;
				if (e.DragUIOverride != null)
				{
					e.DragUIOverride.Caption = "Open File";
					// e.DragUIOverride.IsContentVisible = true;
				}
			}
			catch (Exception ex)
			{
				VM.Log("Error on EditorTabViewDrag: " + ex.Message);
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
			if (e.DataView.Contains(StandardDataFormats.StorageItems) && VM.IsProjectLoaded)
			{
				IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
				foreach (StorageFile file in items)
				{
					var fi = new FileItem(file);
					VM.CurrentProject.Directory.Add(fi);

					if (Path.GetDirectoryName(file.Path) != Path.GetDirectoryName(VM.CurrentProject.Folder.Path))
					{
						await file.CopyAsync(VM.CurrentProject.Folder, file.Name, NameCollisionOption.GenerateUniqueName);
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
				VM.Log("Error on TreeView_DragItemsStarting: " + ex.Message);
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
					VM.OpenFile(fileitem);
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

				if (BitmapsToOpen.Contains(type))
				{
					//await Launcher.LaunchFileAsync(fileitem.File as StorageFile);
					var tip = new TeachingTip() { XamlRoot = XamlRoot, Title = fileitem.FileName, PreferredPlacement = TeachingTipPlacementMode.Center, IsLightDismissEnabled = true };
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
				else
				{
					VM.OpenFile(fileitem);
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
				VM.CurrentRootItem = ei;
				VM.CurrentProject.RootFile = ei.FileName;
			}
			else
			{
				VM.InfoMessage("Warning", "The main file must be in the root folder of the project", InfoBarSeverity.Warning);
			}
		}

		#endregion

		#region Compiler Operations

		public async void CompileTex(bool compileRoot = false, FileItem fileToCompile = null)
		{
			if (!VM.IsSaving)
				try
				{
					VM.IsError = false;
					VM.IsPaused = false;
					VM.IsVisible = true;
					VM.IsSaving = true;

					//await Task.Delay(500);

					string[] modes = new string[] { };
					string[] environments = new string[] { };

					string modesstring = "";
					string environmentsstring = "";
					if (VM.CurrentProject != null)
					{
						modes = VM.CurrentProject.Modes.Where(x => x.IsSelected).Select(x => x.Name).ToArray();
						environments = VM.CurrentProject.Environments.Where(x => x.IsSelected).Select(x => x.Name).ToArray();
					}
					if (modes.Length > 0)
						modesstring = string.Join(",", modes);
					if (environments.Length > 0)
						environmentsstring = string.Join(",", environments);


					FileItem filetocompile = null;
					if (compileRoot)
					{
						FileItem[] root = new FileItem[] { };
						if (VM.CurrentProject != null)
							root = VM.CurrentProject.Directory.FirstOrDefault()?.Children?.Where(x => x.IsRoot)?.ToArray();
						if (root != null && root.Length > 0)
							filetocompile = root.FirstOrDefault();
						else
							filetocompile = fileToCompile ?? VM.CurrentFileItem;
					}
					else
					{
						filetocompile = fileToCompile ?? VM.CurrentFileItem;
					}


					bool compileSuccessful = false;

					try
					{
						string param = "";

						if (VM.IsProjectLoaded)
						{
							if (!string.IsNullOrWhiteSpace(modesstring))
								param += "--mode=" + modesstring + " ";
							if (!string.IsNullOrWhiteSpace(environmentsstring))
								param += "--environment=" + environmentsstring + " ";

							if (VM.CurrentProject.UseSyncTeX)
								param += "--synctex ";
							if (VM.CurrentProject.UseNonStopMode)
								param += "--nonstopmode ";
							if (VM.CurrentProject.UseNoConsole)
								param += "--noconsole ";

							if (VM.CurrentProject.UseInterface)
								param += "--interface=" + VM.CurrentProject.Interface + " ";
							if (VM.CurrentProject.UseParameters)
								param += VM.CurrentProject.AdditionalParameters + " ";
						}

						string logtext = "Running compiler job: " + "context " + param + filetocompile.FileName;
						VM.Log(logtext);

						await Task.Run(async () => { await Compile(filetocompile.FileFolder, filetocompile.FileName, param); });
						compileSuccessful = true;
					}
					catch
					{
						compileSuccessful = false;
					}

					if (compileSuccessful)
					{
						string local = ApplicationData.Current.LocalFolder.Path;
						string curFile = Path.GetFileName(filetocompile.File.Path);
						string filewithoutext = Path.GetFileNameWithoutExtension(curFile);
						string curPDF = filewithoutext + ".pdf";
						string curPDFPath = Path.Combine(filetocompile.File.Path, curPDF);
						string newPathToFile = Path.Combine(local, curPDF);
						StorageFolder currFolder = await StorageFolder.GetFolderFromPathAsync(filetocompile.FileFolder);

						var error = await currFolder.TryGetItemAsync(Path.GetFileNameWithoutExtension(filetocompile.FileName) + "-error.log");
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
							string errorfilename = VM.ConTeXtErrorMessage.filename;
							if (errorfilename.Contains('/'))
								errorfilename = errorfilename.Remove(0, errorfilename.LastIndexOf('/') + 1);
							FileItem erroritem = VM.CurrentProject.GetFileItemByName(VM.CurrentProject.Directory[0], errorfilename);
							if (erroritem != null)
							{
								VM.OpenFile(erroritem);
								await Task.Delay(1000);
								List<SyntaxError> errors = new();
								try
								{
									errors.Add(new() { SyntaxErrorType = SyntaxErrorType.Error, iLine = VM.ConTeXtErrorMessage.linenumber - 1, Title = VM.ConTeXtErrorMessage.lasttexerror });

									VM.Codewriter.SyntaxErrors = errors;
									VM.Codewriter?.SelectLine(new(0, VM.ConTeXtErrorMessage.linenumber - 1));
									VM.Codewriter?.CenterView();
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
							StorageFile pdfout = await currFolder.TryGetItemAsync(Path.GetFileNameWithoutExtension(filetocompile.FileName) + ".pdf") as StorageFile;
							if (pdfout != null)
							{
								VM.Log("Opening " + Path.GetFileNameWithoutExtension(filetocompile.FileName) + ".pdf");
								if (VM.Default.InternalViewer)
								{
									await OpenPDF(pdfout);

									if (VM.CurrentProject.UseSyncTeX)
									{
										string synctexpath = Path.GetFileNameWithoutExtension(filetocompile.FileName) + ".synctex";
										StorageFile synctexfile = await currFolder.TryGetItemAsync(synctexpath) as StorageFile;
										if (synctexfile != null)
										{
											SyncTeX syncTeX = new SyncTeX();
											if (await syncTeX.ParseFile(synctexfile))
											{
												VM.CurrentProject.SyncTeX = syncTeX;
												VM.Log($"Successfully loaded {syncTeX.FileName}");
											}
										}
									}
								}
								else
								{
									await Launcher.LaunchFileAsync(pdfout);
								}
							}
						}

						if (VM.Default.AutoOpenLOG)
						{
							if ((VM.Default.AutoOpenLOGOnlyOnError && error != null) | !VM.Default.AutoOpenLOGOnlyOnError)
							{
								StorageFile logout = await currFolder.TryGetItemAsync(Path.GetFileNameWithoutExtension(filetocompile.FileName) + ".log") as StorageFile;
								if (logout != null)
								{
									FileItem logFile = new FileItem(logout) { };
									VM.OpenFile(logFile);
								}
							}
							else if (VM.Default.AutoOpenLOGOnlyOnError && error == null)
							{
								if (VM.FileItems.Any(x => x.IsLogFile))
								{
									VM.FileItems.Remove(VM.FileItems.First(x => x.IsLogFile));
								}
							}
						}
					}
					VM.Codewriter?.Focus(FocusState.Keyboard);
				}
				catch (Exception f)
				{
					VM.Log("Exception at compile: " + f.Message);
				}
			VM.IsSaving = false;
		}

		public async Task<bool> OpenPDF(StorageFile pdfout)
		{
			try
			{
				VM.IsInternalViewerActive = true;
				await PDFReader.OpenPDF(pdfout);
				return true;
			}
			catch (Exception ex)
			{
				VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
				VM.IsInternalViewerActive = false;
				return false;
			}
		}


		//private async Task<bool> OpenPDF(StorageFile pdfout)
		//{
		//	try
		//	{
		//		Stream stream = await pdfout.OpenStreamForReadAsync();
		//		byte[] buffer = new byte[stream.Length];
		//		stream.Read(buffer, 0, (int)stream.Length);
		//		var asBase64 = Convert.ToBase64String(buffer);
		//		VM.IsInternalViewerActive = true;
		//		// PDFReader.ExecuteScriptAsync("window.openPdfAsBase64('" + asBase64 + "')");
		//		PDFReader.Source = new Uri(pdfout.Path);
		//		return true;
		//	}
		//	catch (Exception ex)
		//	{
		//		VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
		//		VM.IsInternalViewerActive = false;
		//		return false;
		//	}
		//}

		private void PDFReader_NewWindowRequested(WebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
		{
			args.Handled = true;
		}

		private async void Compile_Click(object sender, RoutedEventArgs e)
		{
			FileItem fi = (sender as FrameworkElement).DataContext as FileItem;

			await VM.SaveAll();
			VM.Codewriter?.Save();
			CompileTex(false, fi);
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			await VM.Save((sender as FrameworkElement).DataContext as FileItem);
			VM.Codewriter?.Save();
		}

		private async Task<bool> InstallContext()
		{
			try
			{
				var storageFolder = ApplicationData.Current.LocalFolder;
				VM.Default.ContextDistributionPath = storageFolder.Path;

				var p = Path.Combine(Package.Current.Installed­Location.Path, @"ConTeXt-IDE.Desktop", @"context-mswin.zip");

				StorageFile zipfile;

				if (File.Exists(p))
				{
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
		public async Task<bool> Compile(string workingDirectory, string texFileName, string param)
		{
			Process p = new Process();
			ProcessStartInfo info = new ProcessStartInfo()
			{
				RedirectStandardInput = false,
				RedirectStandardOutput = VM.Default.ShowCompilerOutput,
				RedirectStandardError = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				WorkingDirectory = workingDirectory
			};

			p.StartInfo = info;
			info.FileName = VM.Default.ContextDistributionPath + @"\tex" + getversion() + @"\bin\context.exe";// @"C:\Windows\System32\cmd.exe";
			info.Arguments = " " + param + texFileName;

			if (VM.Default.ShowCompilerOutput)
				p.OutputDataReceived += (a, b) =>
				{
					DispatcherQueue.TryEnqueue(async () =>
																	{
																		VM.Log(b.Data);
																	});

				};

			p.Start();

			if (VM.Default.ShowCompilerOutput)
				p.BeginOutputReadLine();

			p.WaitForExit();
			p.Close();
			return !File.Exists(Path.Combine(workingDirectory, Path.GetFileNameWithoutExtension(texFileName) + @"-error.log"));
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
						await VM.Save(fi);
					}
				}
				if (VM.FileItems.Contains(fi))
					VM.FileItems.Remove(fi);

				//VM.CurrentProject.LastOpenedFiles = VM.FileItems.Select(x => x.FileName).ToList();
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
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
			//return; // Teaching Tips are busted in WinAppSDK 1.0
			if (VM.Default.HelpItemList.Any(x => x.ID == ID))
			{
				var helpItem = VM.Default.HelpItemList[VM.Default.HelpItemList.IndexOf(VM.Default.HelpItemList.First(x => x.ID == ID))];
				if (!helpItem.Shown)
				{
					var tip = new TeachingTip() { XamlRoot = XamlRoot, Title = helpItem.Title, PreferredPlacement = TeachingTipPlacementMode.Center, IsLightDismissEnabled = false, IsOpen = false };
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
			// RichTextBlockHelper.logline = 0;
			// Log.Blocks.Clear();
			VM.CurrentLogLine = 0;
			VM.LogLines.Clear();
		}

		private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "ShowLog":
					if (VM.Default.ShowLog)
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

				case "ShowOutline":
					if (VM.Default.ShowOutline)
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
					if (VM.Default.ShowProjectPane)
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
					if (VM.Default.InternalViewer)
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
									VM.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;



									var proj = new Project(folder.Name, folder);

									if (VM.Default.ProjectList.Any(x => x.Name == proj.Name))
									{
										VM.Default.ProjectList.Remove(VM.Default.ProjectList.First(x => x.Folder.Path == proj.Folder.Path));
									}

									VM.Default.ProjectList.Add(proj);
									VM.CurrentProject = proj;
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


										VM.RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
										string root = Package.Current.InstalledLocation.Path;
										string path = root + @"\Templates";
										if (!Directory.Exists(path))
										{
											path = root + @"\ConTeXt-IDE.Desktop" + @"\Templates";
										}
										var templateFolder = await StorageFolder.GetFolderFromPathAsync(path + @"\" + project);

										await CopyFolderAsync(templateFolder, folder);
										string rootfile = "";

										VM.FileItemsTree?.Clear();
										var proj = new Project(folder.Name, folder);
										switch (project)
										{
											case "mwe": rootfile = "c_main.tex"; break;
											case "projpres": rootfile = "prd_presentation.tex"; proj.Modes.FirstOrDefault(x => x.Name == "screen").IsSelected = true; break;
											case "projthes": rootfile = "prd_thesis.tex"; break;
											case "single": rootfile = "prd_main.tex"; break;
											default: break;
										}

										proj.RootFile = rootfile;


										if (VM.Default.ProjectList.Any(x => x.Name == proj.Name))
										{
											VM.Default.ProjectList.Remove(VM.Default.ProjectList.First(x => x.Folder.Path == proj.Folder.Path));
										}

										VM.Default.ProjectList.Add(proj);
										VM.CurrentProject = proj;

									}
								}
							}
							break;

						default: break;
					}
					VM.PopulateJumpList();
				}
			}
			catch (Exception ex)
			{
				VM.Log("Exception on adding Project: " + ex.Message);
			}
		}

		private async void CbB_Theme_SelectionChanged(object sender, object args)
		{
			await Task.Delay(50);
			SetColor(VM.AccentColor, (ElementTheme)Enum.Parse(typeof(ElementTheme), VM.Default.Theme), true);
			//VM.InfoMessage("You changed the theme", "Not every UI element updates its theme at runtime. You may want to restart the app.");
			await Task.Delay(50);
			PDFReader.Theme = VM.Default.Theme;
		}
		private async void WebView2loading(FrameworkElement sender, object args)
		{
			await ((WebView2)sender).EnsureCoreWebView2Async();
		}
		private void Btndeleteproject_Click(object sender, RoutedEventArgs e)
		{
			try
			{

				var proj = (sender as FrameworkElement).DataContext as Project;

				StorageApplicationPermissions.MostRecentlyUsedList.Remove(proj.Name);
				VM.Default.ProjectList.Remove(proj);

				if (VM.CurrentProject == proj)
					VM.CurrentProject = new Project();

			}
			catch (Exception ex)
			{
				VM.InfoMessage("Error on deleting project", ex.Message, InfoBarSeverity.Error);
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
				VM.InfoMessage("Error on loading project", ex.Message, InfoBarSeverity.Error);
			}
		}

		private void Unload_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				VM.CurrentProject = null;

				VM.FileItems.Clear();
			}
			catch (Exception ex)
			{
				VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
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
				VM.IsSaving = true;
				VM.IsIndeterminate = true;
				VM.InfoMessage("Updating", "Your ConTeXt distribution is getting updated. This can take up to 15 minutes. Do not abort this process!", InfoBarSeverity.Informational);

				bool UpdateSuccessful = false;
				bool temporarilyshowlog = !VM.Default.ShowLog;

				if (temporarilyshowlog)
					VM.Default.ShowLog = true;
				await Task.Run(async () => { UpdateSuccessful = await Update(); });
				if (temporarilyshowlog)
					VM.Default.ShowLog = false;

				if (UpdateSuccessful)
				{
					VM.InfoMessage("Update complete!", "Your ConTeXt distribution is up n' running!", InfoBarSeverity.Success);
				}
				else
				{
					VM.InfoMessage("Error", "Something went wrong. Please try again later.", InfoBarSeverity.Error);
				}
			}
			else
				VM.InfoMessage("No internet connection", "You need to be connected to the internet in order to update your ConTeXt distribution!", InfoBarSeverity.Warning);

			VM.IsSaving = false;
		}

		private async Task<bool> Update()
		{
			try
			{
				Process p = new Process();
				ProcessStartInfo info = new ProcessStartInfo()
				{
					RedirectStandardInput = false,
					RedirectStandardOutput = true,
					RedirectStandardError = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					UseShellExecute = false,
					WorkingDirectory = VM.Default.ContextDistributionPath
				};

				p.StartInfo = info;

				info.FileName = "cmd";
				info.Arguments = "/C install.bat";
				//sw.WriteLine("setx path \"%PATH%;" + VM.Default.ContextDistributionPath + @"\tex\texmf-mswin\bin" + "\"");

				p.OutputDataReceived += (a, b) =>
				{
					DispatcherQueue.TryEnqueue(async () =>
					{
						if (b?.Data != null)
						{
							Match mu = Regex.Match(b.Data, @"updating.*files");
							Match mp = Regex.Match(b.Data, @"(\d+)(\s*\%)");
							if (mu.Success)
							{
								VM.InfoMessage(mu.Value, "Do not abort this operation!");
								VM.ProgressValue = 0;
								VM.IsIndeterminate = false;
							}
							else if (mp.Success)
							{
								int percentage = 0;
								if (int.TryParse(mp.Groups[1]?.Value, out percentage))
								{
									if (percentage <= 100 && percentage >= 0)
										VM.ProgressValue = percentage;
								}
							}
							else
							{
								if (VM.InfoTitle != "Updating")
									VM.InfoMessage("Updating", "Do not abort this operation!");
								VM.IsIndeterminate = true;
							}

							VM.Log(b.Data);
						}
					});

				};

				p.Start();
				p.BeginOutputReadLine();

				p.WaitForExit();
				p.Close();

				return true;
			}
			catch
			{
				return false;
			}
		}

		private void LsV_OutlineItems_SelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			(sender as ListView).ScrollIntoView(args.AddedItems?.FirstOrDefault());
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
			VM.Codewriter?.Save();
			CompileTex();
		}

		private async void Btncompileroot_Click(object sender, RoutedEventArgs e)
		{
			await VM.SaveAll();
			VM.Codewriter?.Save();
			CompileTex(true);
		}


		private void Undo_Click(object sender, SplitButtonClickEventArgs e)
		{
			VM.Codewriter?.TextAction_Undo();
			VM.Codewriter?.Focus(FocusState.Keyboard);
		}

		private async void Btnsave_Click(object sender, RoutedEventArgs e)
		{
			await VM.Save();
			VM.Codewriter?.Save();
		}

		private async void btnsaveall_Click(object sender, RoutedEventArgs e)
		{
			await VM.SaveAll();
			VM.Codewriter?.Save();
		}

		private void Modes_Click(object sender, RoutedEventArgs e)
		{
			ShowTeachingTip("Modes", sender);
		}

		private void Environments_Click(object sender, RoutedEventArgs e)
		{
			ShowTeachingTip("Environments", sender);
		}
		private async void Restore_Click(object sender, RoutedEventArgs e)
		{
			var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Restore default settings", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
			TextBlock tb = new() { Text = "Are you sure to reset the app settings? You will need to restart the app and you will need to manually add your projects again." };
			cd.Content = tb;
			if (await cd.ShowAsync() == ContentDialogResult.Primary)
			{
				StorageApplicationPermissions.MostRecentlyUsedList.Clear();
				VM.Default.ProjectList.Clear();
				Settings.RestoreSettings();
				Unload_Click(null, null);
			}
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
				VM.CurrentProject.Modes.Add(mode);
				VM.Default.SaveSettings();
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
				VM.Log("Opening " + lsf.Path + hf.Path + hf.FileName);
				var sf = await StorageFile.GetFileFromPathAsync(lsf.Path + hf.Path + hf.FileName);

				if (VM.Default.HelpPDFInInternalViewer)
				{
					await OpenPDF(sf);
				}
				else
					await Launcher.LaunchFileAsync(sf);
			}
			catch (Exception ex)
			{
				VM.Log("Exception on Help Item Click: " + ex.Message);
			}
		}

		private void LsV_OutlineItems_ItemClick(object sender, ItemClickEventArgs e)
		{
			try
			{
				if (VM.Codewriter != null)
				{
					VM.Codewriter.Selection = new(new Place(0, (e.ClickedItem as OutlineItem).Row - 1));
					VM.Codewriter.CenterView();
				}
			}
			catch (Exception ex)
			{
				VM.Log("Exception on Outline Item Click: " + ex.Message);
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
				if (VM.Default.FontSize < 100)
					VM.Default.FontSize++;
			}
			else if (VM.Default.FontSize > 6)
			{
				VM.Default.FontSize--;
			}
			e.Handled = true;
		}
		private void Tbx_TabLength_Wheel(object sender, PointerRoutedEventArgs e)
		{
			var point = e.GetCurrentPoint(sender as UIElement);
			int wheeldelta = point.Properties.MouseWheelDelta;
			if (wheeldelta > 0)
			{
				if (VM.Default.TabLength < 5)
					VM.Default.TabLength++;
			}
			else if (VM.Default.TabLength > 0)
			{
				VM.Default.TabLength--;
			}
			e.Handled = true;
		}

		#endregion

		#region Ribbon : View

		private void ColorsGridView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is AccentColor accentColor)
			{
				SetColor(accentColor, RequestedTheme, true);
				VM.Default.AccentColor = accentColor.Name;
			}
		}

		public void SetColor(AccentColor accentColor = null, ElementTheme theme = ElementTheme.Dark, bool reload = true)
		{
			var setting = ((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]);
			if (accentColor != null)
			{
				setting.Theme = theme;
				setting.AccentColor = accentColor.Color;
				Application.Current.Resources["SystemAccentColor"] = accentColor.Color;
				Application.Current.Resources["SystemAccentColorLight2"] = setting.AccentColorLow;
				Application.Current.Resources["SystemAccentColorDark1"] = setting.AccentColorLow.ChangeColorBrightness(0.1f);

			}

			// App.Current.Resources.MergedDictionaries.Add(palette);

			if (App.mainWindow.IsCustomizationSupported)
			{
				App.mainWindow.AW.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
				App.mainWindow.AW.TitleBar.ButtonBackgroundColor = Colors.Transparent;
				App.mainWindow.AW.TitleBar.ButtonHoverBackgroundColor = setting.AccentColor;
				App.mainWindow.AW.TitleBar.ButtonForegroundColor = theme == ElementTheme.Light ? Colors.Black : Colors.White;
				App.mainWindow.AW.TitleBar.ButtonInactiveForegroundColor = theme == ElementTheme.Light ? Color.FromArgb(255, 50, 50, 50) : Color.FromArgb(255, 200, 200, 200);
			}
			else
			{
				Application.Current.Resources["WindowCaptionBackground"] = setting.AccentColorLow;
				Application.Current.Resources["WindowCaptionBackgroundDisabled"] = setting.AccentColorLow;
			}

			if (reload)
			{
				ReloadPageTheme(theme);
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
			if (VM.Default.Theme == "Dark")
				VM.Default.Theme = "Light";
			else if (VM.Default.Theme == "Light")
				VM.Default.Theme = "Default";
			else if (VM.Default.Theme == "Default")
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
				SetColor(VM.AccentColor, RequestedTheme, true);
			}
			else
			{
				VM.AccentColor = null;
				SetColor(new("Default", VM.SystemAccentColor), RequestedTheme);
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
			else
				VM.InfoMessage("Download error", "No internet connection available.");
		}

		private async void Btn_RemoveModule_Click(object sender, RoutedEventArgs e)
		{
			VM.IsIndeterminate = true;
			VM.IsSaving = true;
			VM.InfoMessage("Removing module", "Please wait...");
			ContextModule module = (sender as FrameworkElement).DataContext as ContextModule;

			string modulepath = Path.Combine(ApplicationData.Current.LocalFolder.Path, @"tex\texmf\tex\context\third\", module.Name + @"\");

			if (Directory.Exists(modulepath))
			{
				await (await StorageFolder.GetFolderFromPathAsync(modulepath)).DeleteAsync();
			}

			module.IsInstalled = false;

			VM.Default.SaveSettings();

			bool temporarilyshowlog = !VM.Default.ShowLog;
			bool success = false;

			if (temporarilyshowlog)
				VM.Default.ShowLog = true;
			await Task.Run(async () => { success = await Generate(); });
			if (temporarilyshowlog)
				VM.Default.ShowLog = false;

			VM.IsSaving = false;
			if (success)
			{
				VM.InfoMessage("Success", $"Module {module.Name} has been successfully uninstalled.", InfoBarSeverity.Success);
			}
			else
			{
				VM.InfoMessage("Error", $"Module {module.Name} could not be uninstalled.", InfoBarSeverity.Error);
			}
		}

		private void DownloadModule(ContextModule module)
		{
			try
			{
				VM.IsIndeterminate = false;
				VM.IsSaving = true;

				VM.InfoMessage($"Downloading Module {module.Name}", "Please wait...");
				using (WebClient wc = new WebClient())
				{
					string filepath = Path.Combine(ApplicationData.Current.LocalFolder.Path, module.Name + ".zip");

					if (File.Exists(filepath))
					{
						File.Delete(filepath);
					}

					bool success = false;
					wc.DownloadProgressChanged += (a, b) => { VM.ProgressValue = b.ProgressPercentage; };

					wc.DownloadFileCompleted += async (a, b) =>
					{
						VM.IsIndeterminate = true;
						VM.InfoMessage($"Installing Module {module.Name}", "Please wait...");
						success = await InstallModule(module, filepath);

						VM.IsSaving = false;

						module.IsInstalled = true;

						if (success)
						{
							VM.InfoMessage("Success", $"Module {module.Name} has been successfully installed.", InfoBarSeverity.Success);
						}
						else
						{
							VM.InfoMessage("Error", $"Module {module.Name} could not be installed.", InfoBarSeverity.Error);
						}
						VM.Default.SaveSettings();
					};
					wc.DownloadFileAsync(new System.Uri(module.URL), filepath);
				}
			}
			catch (Exception ex)
			{
				VM.InfoMessage("Download Error", ex.Message, InfoBarSeverity.Error);
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
					// VM.InfoMessage(folder.Path);
					var extractionfolder = await StorageFolder.GetFolderFromPathAsync(extractionpath);
					// VM.InfoMessage(extractionfolder.Path);

					await CopyFolderAsync(folder, extractionfolder);

				}


				bool UpdateSuccessful = false;
				bool temporarilyshowlog = !VM.Default.ShowLog;

				if (temporarilyshowlog)
					VM.Default.ShowLog = true;
				await Task.Run(async () => { UpdateSuccessful = await Generate(); });
				if (temporarilyshowlog)
					VM.Default.ShowLog = false;

				return UpdateSuccessful;
			}
			catch (Exception ex)
			{
				VM.InfoMessage("Install Error", ex.Message, InfoBarSeverity.Error);
				return false;
			}
		}

		private async Task<bool> Generate()
		{
			try
			{
				Process p = new Process();
				ProcessStartInfo info = new ProcessStartInfo()
				{
					RedirectStandardInput = false,
					RedirectStandardOutput = true,
					RedirectStandardError = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					UseShellExecute = false,
					WorkingDirectory = VM.Default.ContextDistributionPath
				};

				info.FileName = VM.Default.ContextDistributionPath + @"\tex" + getversion() + @"\bin\context.exe";
				info.Arguments = "--make --generate";
				p.StartInfo = info;

				//sw.WriteLine("setx path \"%PATH%;" + VM.Default.ContextDistributionPath + @"\tex\texmf-mswin\bin" + "\"");

				p.OutputDataReceived += (a, b) =>
				{
					DispatcherQueue.TryEnqueue(async () =>
					{
						VM.Log(b.Data);
					});

				};

				p.Start();
				p.BeginOutputReadLine();

				p.WaitForExit();
				int exit = p.ExitCode;
				p.Close();

				return exit == 0;
			}
			catch
			{
				return false;
			}
		}

		private List<string> disableGroups = new List<string>() { "attribute", "background", "background colors", "boxes", "buffer", "catcode", "characters", "commandhandler", "strings", "symbols", "tracker", "twopassdata", "verbatim", "xml" };
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
							foreach (var constant in instancecommand?.Instances?.Constant)
							{
								string sequence = "";
								if (instancecommand?.Sequence?.String?.Count > 0)
									sequence = instancecommand?.Sequence?.String?.First()?.Value ?? "";
								Command newcommand = (Command)instancecommand.Clone();
								newcommand.Name = sequence + constant.Value;
								commands.Insert(commands.IndexOf(instancecommand), newcommand);
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
							foreach (Models.Argument argument in command?.Arguments?.ArgumentsList)
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
						VM.Default.CommandGroups = VM.ContextCommandGroupList.Select(x => new CommandGroup() { Name = x, IsSelected = !disableGroups.Contains(x) }).ToList();
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
				VM.InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
			}

		}

		private void PopulateIntelliSense(string name)
		{
			var lang = FileLanguages.LanguageList.First(x => x.Name == name);
			if (lang.Commands == null)
				lang.Commands = new();
			lang.Commands.Clear();
			foreach (Command command in contextcommands)
			{
				string snippet = "";
				string commandname = @"\" + (command.Type == "environment" ? "start" : "") + command.Name;
				if (command.Type == "environment")
				{
					if (command?.Sequence?.Instance?.Value == "section")
						snippet = "[title={}]";
					else
					{
						switch (command?.Name)
						{
							case "mode": snippet = "[]"; break;
							case "placeformula": snippet = "[reference=eq:]"; break;
							case "placefigure": snippet = "[title={}, location=here, reference=fig:]"; break;
							case "placetable": snippet = "[title={}, location=here, reference=tab:]"; break;
							case "component": snippet = "[]"; break;
							case "environment": snippet = "[]"; break;
							case "product": snippet = "[]"; break;
							case "project": snippet = "[]"; break;
						}
					}
					snippet += "\n\t\n\\stop" + command.Name;
				}
				else
				{
					switch (command?.Name)
					{
						case "cite": snippet = "[]"; break;
						case "color": snippet = "[]{}"; break;
						case "blank": snippet = "[medium]"; break;
						case "unit": snippet = "{}"; break;

						case "frac": snippet = "{}"; break;

						case "component": snippet = "[]"; break;
						case "environment": snippet = "[]"; break;
						case "product": snippet = "[]"; break;
						case "project": snippet = "[]"; break;

						case "definecolor": snippet = "[r=1, g=1, b=1]"; break;
						case "definestructureconversionset": snippet = "[frontpart:pagenumber][][Romannumerals]"; break;

						case "setupinteraction": snippet = @"[title={}, subtitle={}, author={}, date={}, state=start, focus=standard, style=, click=yes, color=, contrastcolor=]"; break;
						case "placebookmarks": snippet = @"[chapter,section,subsection,subject][force=yes]"; break;

						case "setuplayout": snippet = "[topspace=2cm, backspace=2cm, header=24pt, footer=36pt, height=middle, width=middle]"; break;
						case "setuppapersize": snippet = "[A4][A4]"; break;
						case "setuppapenumbering": snippet = "[alternative=siglesided]"; break;
						case "setupwhitespace": snippet = "[medium]"; break;
						case "setupindenting": snippet = "[no]"; break;
						case "setupbodyfont": snippet = "[sans, 11pt]"; break;
						case "setupfooter": snippet = @"[style=\tf]"; break;
						case "setupheader": snippet = @"[style=\tf]"; break;
						case "setuphead": snippet = @"[section][style=\bfc, before=\bigskip, after=\medskip, page=yes, header=empty]"; break;
					}
				}
				if ((command.Type == "environment" && VM.Default.SuggestStartStop) | (command.Type != "environment" && VM.Default.SuggestCommands))
				{
					IntelliSense intelliSense = new(commandname) { Snippet = snippet, Description = "Category: " + command.Category, IntelliSenseType = IntelliSenseType.Command };
					if (VM.Default.SuggestArguments && command.Arguments?.ArgumentsList != null)
						foreach (var item in command.Arguments?.ArgumentsList)
						{
							if (item is Assignments assignments)
							{
								intelliSense.ArgumentsList.Add(new()
								{
									Delimiters = assignments?.Delimiters,
									IntelliSenseType = IntelliSenseType.Argument,
									Name = assignments?.Inherit?.Name,
									Parameters = assignments?.Parameter?.Select(x => new CodeEditorControl_WinUI.Parameter() { Name = x.Name, Description = "Type: " + x.Constant?.FirstOrDefault()?.Type, Constant = x.Constant?.Select(x => new CodeEditorControl_WinUI.Constant() { Type = x.Type }).ToList() }).ToList(),
								});
							}
						}
					intelliSense.Token = command.Type == "environment" ? Token.Environment : Token.Command;
					//intelliSense.ArgumentsList = command.Arguments?.ArgumentsList?.Select(x=> x is Assignments assignments ? new CodeEditorControl_WinUI.() { Delimiters = x.Delimiters, List = x.List, Name = assignments.Inherit?.Name, } : null ).ToList();
					lang.Commands.Add(intelliSense);
				}
			}
			if (VM.Default.SuggestPrimitives)
			{
				string[] primitives = new string[] { "year", "xtokspre", "xtoksapp", "xspaceskip", "xleaders", "xdef", "write", "wordboundary", "widowpenalty", "widowpenalties", "wd", "vtop", "vss", "vsplit", "vskip", "vsize", "vrule", "vpack", "voffset", "vfuzz", "vfilneg", "vfill", "vfil", "vcenter", "vbox", "vbadness", "valign", "vadjust", "useimageresource", "useboxresource", "uppercase", "unvcopy", "unvbox", "unskip", "unpenalty", "unless", "unkern", "uniformdeviate", "unhcopy", "unhbox", "underline", "uchyph", "uccode", "tracingstats", "tracingscantokens", "tracingrestores", "tracingparagraphs", "tracingpages", "tracingoutput", "tracingonline", "tracingnesting", "tracingmacros", "tracinglostchars", "tracingifs", "tracinggroups", "tracingfonts", "tracingcommands", "tracingassigns", "tpack", "topskip", "topmarks", "topmark", "tolerance", "tokspre", "toksdef", "toksapp", "toks", "time", "thinmuskip", "thickmuskip", "the", "textstyle", "textfont", "textdirection", "textdir", "tagcode", "tabskip", "synctex", "suppressprimitiveerror", "suppressoutererror", "suppressmathparerror", "suppresslongerror", "suppressifcsnameerror", "suppressfontnotfounderror", "string", "splittopskip", "splitmaxdepth", "splitfirstmarks", "splitfirstmark", "splitdiscards", "splitbotmarks", "splitbotmark", "special", "span", "spaceskip", "spacefactor", "skipdef", "skip", "skewchar", "showtokens", "showthe", "showlists", "showifs", "showgroups", "showboxdepth", "showboxbreadth", "showbox", "show", "shipout", "shapemode", "sfcode", "setrandomseed", "setlanguage", "setfontid", "setbox", "scrollmode", "scriptstyle", "scriptspace", "scriptscriptstyle", "scriptscriptfont", "scriptfont", "scantokens", "scantextokens", "savingvdiscards", "savinghyphcodes", "savepos", "saveimageresource", "savecatcodetable", "saveboxresource", "rpcode", "romannumeral", "rightskip", "rightmarginkern", "righthyphenmin", "rightghost", "right", "relpenalty", "relax", "readline", "read", "randomseed", "raise", "radical", "quitvmode", "pxdimen", "protrusionboundary", "protrudechars", "primitive", "prevgraf", "prevdepth", "pretolerance", "prerelpenalty", "prehyphenchar", "preexhyphenchar", "predisplaysize", "predisplaypenalty", "predisplaygapfactor", "predisplaydirection", "prebinoppenalty", "posthyphenchar", "postexhyphenchar", "postdisplaypenalty", "penalty", "pdfximage", "pdfxformresources", "pdfxformname", "pdfxformmargin", "pdfxformattr", "pdfxform", "pdfvorigin", "pdfvariable", "pdfuniqueresname", "pdfuniformdeviate", "pdftrailerid", "pdftrailer", "pdftracingfonts", "pdfthreadmargin", "pdfthread", "pdftexversion", "pdftexrevision", "pdftexbanner", "pdfsuppressptexinfo", "pdfsuppressoptionalinfo", "pdfstartthread", "pdfstartlink", "pdfsetrandomseed", "pdfsetmatrix", "pdfsavepos", "pdfsave", "pdfretval", "pdfrestore", "pdfreplacefont", "pdfrefximage", "pdfrefxform", "pdfrefobj", "pdfrecompress", "pdfrandomseed", "pdfpxdimen", "pdfprotrudechars", "pdfprimitive", "pdfpkresolution", "pdfpkmode", "pdfpkfixeddpi", "pdfpagewidth", "pdfpagesattr", "pdfpageresources", "pdfpageref", "pdfpageheight", "pdfpagebox", "pdfpageattr", "pdfoutput", "pdfoutline", "pdfomitcidset", "pdfomitcharset", "pdfobjcompresslevel", "pdfobj", "pdfnormaldeviate", "pdfnoligatures", "pdfnames", "pdfminorversion", "pdfmapline", "pdfmapfile", "pdfmajorversion", "pdfliteral", "pdflinkmargin", "pdflastypos", "pdflastxpos", "pdflastximagepages", "pdflastximage", "pdflastxform", "pdflastobj", "pdflastlink", "pdflastlinedepth", "pdflastannot", "pdfinsertht", "pdfinfoomitdate", "pdfinfo", "pdfinclusionerrorlevel", "pdfinclusioncopyfonts", "pdfincludechars", "pdfimageresolution", "pdfimagehicolor", "pdfimagegamma", "pdfimageapplygamma", "pdfimageaddfilename", "pdfignoreunknownimages", "pdfignoreddimen", "pdfhorigin", "pdfglyphtounicode", "pdfgentounicode", "pdfgamma", "pdffontsize", "pdffontobjnum", "pdffontname", "pdffontexpand", "pdffontattr", "pdffirstlineheight", "pdffeedback", "pdfextension", "pdfendthread", "pdfendlink", "pdfeachlineheight", "pdfeachlinedepth", "pdfdraftmode", "pdfdestmargin", "pdfdest", "pdfdecimaldigits", "pdfcreationdate", "pdfcopyfont", "pdfcompresslevel", "pdfcolorstackinit", "pdfcolorstack", "pdfcatalog", "pdfannot", "pdfadjustspacing", "pausing", "patterns", "parskip", "parshapelength", "parshapeindent", "parshapedimen", "parshape", "parindent", "parfillskip", "pardirection", "pardir", "par", "pagewidth", "pagetotal", "pagetopoffset", "pagestretch", "pageshrink", "pagerightoffset", "pageleftoffset", "pageheight", "pagegoal", "pagefilstretch", "pagefillstretch", "pagefilllstretch", "pagediscards", "pagedirection", "pagedir", "pagedepth", "pagebottomoffset", "overwithdelims", "overline", "overfullrule", "over", "outputpenalty", "outputmode", "outputbox", "output", "outer", "or", "openout", "openin", "omit", "numexpr", "number", "nullfont", "nulldelimiterspace", "novrule", "nospaces", "normalyear", "normalxtokspre", "normalxtoksapp", "normalxspaceskip", "normalxleaders", "normalxdef", "normalwrite", "normalwordboundary", "normalwidowpenalty", "normalwidowpenalties", "normalwd", "normalvtop", "normalvss", "normalvsplit", "normalvskip", "normalvsize", "normalvrule", "normalvpack", "normalvoffset", "normalvfuzz", "normalvfilneg", "normalvfill", "normalvfil", "normalvcenter", "normalvbox", "normalvbadness", "normalvalign", "normalvadjust", "normaluseimageresource", "normaluseboxresource", "normaluppercase", "normalunvcopy", "normalunvbox", "normalunskip", "normalunpenalty", "normalunless", "normalunkern", "normaluniformdeviate", "normalunhcopy", "normalunhbox", "normalunexpanded", "normalunderline", "normaluchyph", "normaluccode", "normaltracingstats", "normaltracingscantokens", "normaltracingrestores", "normaltracingparagraphs", "normaltracingpages", "normaltracingoutput", "normaltracingonline", "normaltracingnesting", "normaltracingmacros", "normaltracinglostchars", "normaltracingifs", "normaltracinggroups", "normaltracingfonts", "normaltracingcommands", "normaltracingassigns", "normaltpack", "normaltopskip", "normaltopmarks", "normaltopmark", "normaltolerance", "normaltokspre", "normaltoksdef", "normaltoksapp", "normaltoks", "normaltime", "normalthinmuskip", "normalthickmuskip", "normalthe", "normaltextstyle", "normaltextfont", "normaltextdirection", "normaltextdir", "normaltagcode", "normaltabskip", "normalsynctex", "normalsuppressprimitiveerror", "normalsuppressoutererror", "normalsuppressmathparerror", "normalsuppresslongerror", "normalsuppressifcsnameerror", "normalsuppressfontnotfounderror", "normalstring", "normalsplittopskip", "normalsplitmaxdepth", "normalsplitfirstmarks", "normalsplitfirstmark", "normalsplitdiscards", "normalsplitbotmarks", "normalsplitbotmark", "normalspecial", "normalspan", "normalspaceskip", "normalspacefactor", "normalskipdef", "normalskip", "normalskewchar", "normalshowtokens", "normalshowthe", "normalshowlists", "normalshowifs", "normalshowgroups", "normalshowboxdepth", "normalshowboxbreadth", "normalshowbox", "normalshow", "normalshipout", "normalshapemode", "normalsfcode", "normalsetrandomseed", "normalsetlanguage", "normalsetfontid", "normalsetbox", "normalscrollmode", "normalscriptstyle", "normalscriptspace", "normalscriptscriptstyle", "normalscriptscriptfont", "normalscriptfont", "normalscantokens", "normalscantextokens", "normalsavingvdiscards", "normalsavinghyphcodes", "normalsavepos", "normalsaveimageresource", "normalsavecatcodetable", "normalsaveboxresource", "normalrpcode", "normalromannumeral", "normalrightskip", "normalrightmarginkern", "normalrighthyphenmin", "normalrightghost", "normalright", "normalrelpenalty", "normalrelax", "normalreadline", "normalread", "normalrandomseed", "normalraise", "normalradical", "normalquitvmode", "normalpxdimen", "normalprotrusionboundary", "normalprotrudechars", "normalprotected", "normalprimitive", "normalprevgraf", "normalprevdepth", "normalpretolerance", "normalprerelpenalty", "normalprehyphenchar", "normalpreexhyphenchar", "normalpredisplaysize", "normalpredisplaypenalty", "normalpredisplaygapfactor", "normalpredisplaydirection", "normalprebinoppenalty", "normalposthyphenchar", "normalpostexhyphenchar", "normalpostdisplaypenalty", "normalpenalty", "normalpdfximage", "normalpdfxformresources", "normalpdfxformname", "normalpdfxformmargin", "normalpdfxformattr", "normalpdfxform", "normalpdfvorigin", "normalpdfvariable", "normalpdfuniqueresname", "normalpdfuniformdeviate", "normalpdftrailerid", "normalpdftrailer", "normalpdftracingfonts", "normalpdfthreadmargin", "normalpdfthread", "normalpdftexversion", "normalpdftexrevision", "normalpdftexbanner", "normalpdfsuppressptexinfo", "normalpdfsuppressoptionalinfo", "normalpdfstartthread", "normalpdfstartlink", "normalpdfsetrandomseed", "normalpdfsetmatrix", "normalpdfsavepos", "normalpdfsave", "normalpdfretval", "normalpdfrestore", "normalpdfreplacefont", "normalpdfrefximage", "normalpdfrefxform", "normalpdfrefobj", "normalpdfrecompress", "normalpdfrandomseed", "normalpdfpxdimen", "normalpdfprotrudechars", "normalpdfprimitive", "normalpdfpkresolution", "normalpdfpkmode", "normalpdfpkfixeddpi", "normalpdfpagewidth", "normalpdfpagesattr", "normalpdfpageresources", "normalpdfpageref", "normalpdfpageheight", "normalpdfpagebox", "normalpdfpageattr", "normalpdfoutput", "normalpdfoutline", "normalpdfomitcidset", "normalpdfomitcharset", "normalpdfobjcompresslevel", "normalpdfobj", "normalpdfnormaldeviate", "normalpdfnoligatures", "normalpdfnames", "normalpdfminorversion", "normalpdfmapline", "normalpdfmapfile", "normalpdfmajorversion", "normalpdfliteral", "normalpdflinkmargin", "normalpdflastypos", "normalpdflastxpos", "normalpdflastximagepages", "normalpdflastximage", "normalpdflastxform", "normalpdflastobj", "normalpdflastlink", "normalpdflastlinedepth", "normalpdflastannot", "normalpdfinsertht", "normalpdfinfoomitdate", "normalpdfinfo", "normalpdfinclusionerrorlevel", "normalpdfinclusioncopyfonts", "normalpdfincludechars", "normalpdfimageresolution", "normalpdfimagehicolor", "normalpdfimagegamma", "normalpdfimageapplygamma", "normalpdfimageaddfilename", "normalpdfignoreunknownimages", "normalpdfignoreddimen", "normalpdfhorigin", "normalpdfglyphtounicode", "normalpdfgentounicode", "normalpdfgamma", "normalpdffontsize", "normalpdffontobjnum", "normalpdffontname", "normalpdffontexpand", "normalpdffontattr", "normalpdffirstlineheight", "normalpdffeedback", "normalpdfextension", "normalpdfendthread", "normalpdfendlink", "normalpdfeachlineheight", "normalpdfeachlinedepth", "normalpdfdraftmode", "normalpdfdestmargin", "normalpdfdest", "normalpdfdecimaldigits", "normalpdfcreationdate", "normalpdfcopyfont", "normalpdfcompresslevel", "normalpdfcolorstackinit", "normalpdfcolorstack", "normalpdfcatalog", "normalpdfannot", "normalpdfadjustspacing", "normalpausing", "normalpatterns", "normalparskip", "normalparshapelength", "normalparshapeindent", "normalparshapedimen", "normalparshape", "normalparindent", "normalparfillskip", "normalpardirection", "normalpardir", "normalpar", "normalpagewidth", "normalpagetotal", "normalpagetopoffset", "normalpagestretch", "normalpageshrink", "normalpagerightoffset", "normalpageleftoffset", "normalpageheight", "normalpagegoal", "normalpagefilstretch", "normalpagefillstretch", "normalpagefilllstretch", "normalpagediscards", "normalpagedirection", "normalpagedir", "normalpagedepth", "normalpagebottomoffset", "normaloverwithdelims", "normaloverline", "normaloverfullrule", "normalover", "normaloutputpenalty", "normaloutputmode", "normaloutputbox", "normaloutput", "normalouter", "normalor", "normalopenout", "normalopenin", "normalomit", "normalnumexpr", "normalnumber", "normalnullfont", "normalnulldelimiterspace", "normalnovrule", "normalnospaces", "normalnormaldeviate", "normalnonstopmode", "normalnonscript", "normalnolimits", "normalnoligs", "normalnokerns", "normalnoindent", "normalnohrule", "normalnoexpand", "normalnoboundary", "normalnoalign", "normalnewlinechar", "normalmutoglue", "normalmuskipdef", "normalmuskip", "normalmultiply", "normalmuexpr", "normalmskip", "normalmoveright", "normalmoveleft", "normalmonth", "normalmkern", "normalmiddle", "normalmessage", "normalmedmuskip", "normalmeaning", "normalmaxdepth", "normalmaxdeadcycles", "normalmathsurroundskip", "normalmathsurroundmode", "normalmathsurround", "normalmathstyle", "normalmathscriptsmode", "normalmathscriptcharmode", "normalmathscriptboxmode", "normalmathrulethicknessmode", "normalmathrulesmode", "normalmathrulesfam", "normalmathrel", "normalmathpunct", "normalmathpenaltiesmode", "normalmathord", "normalmathoption", "normalmathopen", "normalmathop", "normalmathnolimitsmode", "normalmathitalicsmode", "normalmathinner", "normalmathflattenmode", "normalmatheqnogapstep", "normalmathdisplayskipmode", "normalmathdirection", "normalmathdir", "normalmathdelimitersmode", "normalmathcode", "normalmathclose", "normalmathchoice", "normalmathchardef", "normalmathchar", "normalmathbin", "normalmathaccent", "normalmarks", "normalmark", "normalmag", "normalluatexversion", "normalluatexrevision", "normalluatexbanner", "normalluafunctioncall", "normalluafunction", "normalluaescapestring", "normalluadef", "normalluacopyinputnodes", "normalluabytecodecall", "normalluabytecode", "normallpcode", "normallowercase", "normallower", "normallooseness", "normallong", "normallocalrightbox", "normallocalleftbox", "normallocalinterlinepenalty", "normallocalbrokenpenalty", "normallinepenalty", "normallinedirection", "normallinedir", "normallimits", "normalletterspacefont", "normalletcharcode", "normallet", "normalleqno", "normalleftskip", "normalleftmarginkern", "normallefthyphenmin", "normalleftghost", "normalleft", "normalleaders", "normallccode", "normallateluafunction", "normallatelua", "normallastypos", "normallastxpos", "normallastskip", "normallastsavedimageresourcepages", "normallastsavedimageresourceindex", "normallastsavedboxresourceindex", "normallastpenalty", "normallastnodetype", "normallastnamedcs", "normallastlinefit", "normallastkern", "normallastbox", "normallanguage", "normalkern", "normaljobname", "normalinterlinepenalty", "normalinterlinepenalties", "normalinteractionmode", "normalinsertpenalties", "normalinsertht", "normalinsert", "normalinputlineno", "normalinput", "normalinitcatcodetable", "normalindent", "normalimmediateassignment", "normalimmediateassigned", "normalimmediate", "normalignorespaces", "normalignoreligaturesinfont", "normalifx", "normalifvoid", "normalifvmode", "normalifvbox", "normaliftrue", "normalifprimitive", "normalifpdfprimitive", "normalifpdfabsnum", "normalifpdfabsdim", "normalifodd", "normalifnum", "normalifmmode", "normalifinner", "normalifincsname", "normalifhmode", "normalifhbox", "normaliffontchar", "normaliffalse", "normalifeof", "normalifdim", "normalifdefined", "normalifcsname", "normalifcondition", "normalifcat", "normalifcase", "normalifabsnum", "normalifabsdim", "normalif", "normalhyphenpenaltymode", "normalhyphenpenalty", "normalhyphenchar", "normalhyphenationmin", "normalhyphenationbounds", "normalhyphenation", "normalht", "normalhss", "normalhskip", "normalhsize", "normalhrule", "normalhpack", "normalholdinginserts", "normalhoffset", "normalhjcode", "normalhfuzz", "normalhfilneg", "normalhfill", "normalhfil", "normalhbox", "normalhbadness", "normalhangindent", "normalhangafter", "normalhalign", "normalgtokspre", "normalgtoksapp", "normalgluetomu", "normalgluestretchorder", "normalgluestretch", "normalglueshrinkorder", "normalglueshrink", "normalglueexpr", "normalglobaldefs", "normalglobal", "normalglet", "normalgleaders", "normalgdef", "normalfuturelet", "normalfutureexpandis", "normalfutureexpand", "normalformatname", "normalfontname", "normalfontid", "normalfontdimen", "normalfontcharwd", "normalfontcharic", "normalfontcharht", "normalfontchardp", "normalfont", "normalfloatingpenalty", "normalfixupboxesmode", "normalfirstvalidlanguage", "normalfirstmarks", "normalfirstmark", "normalfinalhyphendemerits", "normalfi", "normalfam", "normalexplicithyphenpenalty", "normalexplicitdiscretionary", "normalexpandglyphsinfont", "normalexpanded", "normalexpandafter", "normalexhyphenpenalty", "normalexhyphenchar", "normalexceptionpenalty", "normaleveryvbox", "normaleverypar", "normaleverymath", "normaleveryjob", "normaleveryhbox", "normaleveryeof", "normaleverydisplay", "normaleverycr", "normaletokspre", "normaletoksapp", "normalescapechar", "normalerrorstopmode", "normalerrorcontextlines", "normalerrmessage", "normalerrhelp", "normaleqno", "normalendlocalcontrol", "normalendlinechar", "normalendinput", "normalendgroup", "normalendcsname", "normalend", "normalemergencystretch", "normalelse", "normalefcode", "normaledef", "normaleTeXversion", "normaleTeXrevision", "normaleTeXminorversion", "normaleTeXVersion", "normaldvivariable", "normaldvifeedback", "normaldviextension", "normaldump", "normaldraftmode", "normaldp", "normaldoublehyphendemerits", "normaldivide", "normaldisplaywidth", "normaldisplaywidowpenalty", "normaldisplaywidowpenalties", "normaldisplaystyle", "normaldisplaylimits", "normaldisplayindent", "normaldiscretionary", "normaldirectlua", "normaldimexpr", "normaldimendef", "normaldimen", "normaldeviate", "normaldetokenize", "normaldelimitershortfall", "normaldelimiterfactor", "normaldelimiter", "normaldelcode", "normaldefaultskewchar", "normaldefaulthyphenchar", "normaldef", "normaldeadcycles", "normalday", "normalcurrentiftype", "normalcurrentiflevel", "normalcurrentifbranch", "normalcurrentgrouptype", "normalcurrentgrouplevel", "normalcsstring", "normalcsname", "normalcrcr", "normalcrampedtextstyle", "normalcrampedscriptstyle", "normalcrampedscriptscriptstyle", "normalcrampeddisplaystyle", "normalcr", "normalcountdef", "normalcount", "normalcopyfont", "normalcopy", "normalcompoundhyphenmode", "normalclubpenalty", "normalclubpenalties", "normalcloseout", "normalclosein", "normalclearmarks", "normalcleaders", "normalchardef", "normalchar", "normalcatcodetable", "normalcatcode", "normalbrokenpenalty", "normalbreakafterdirmode", "normalboxmaxdepth", "normalboxdirection", "normalboxdir", "normalbox", "normalboundary", "normalbotmarks", "normalbotmark", "normalbodydirection", "normalbodydir", "normalbinoppenalty", "normalbelowdisplayskip", "normalbelowdisplayshortskip", "normalbegingroup", "normalbegincsname", "normalbatchmode", "normalbadness", "normalautomatichyphenpenalty", "normalautomatichyphenmode", "normalautomaticdiscretionary", "normalattributedef", "normalattribute", "normalatopwithdelims", "normalatop", "normalaligntab", "normalalignmark", "normalaftergroup", "normalafterassignment", "normaladvance", "normaladjustspacing", "normaladjdemerits", "normalaccent", "normalabovewithdelims", "normalabovedisplayskip", "normalabovedisplayshortskip", "normalabove", "normalXeTeXversion", "normalUvextensible", "normalUunderdelimiter", "normalUsuperscript", "normalUsubscript", "normalUstopmath", "normalUstopdisplaymath", "normalUstartmath", "normalUstartdisplaymath", "normalUstack", "normalUskewedwithdelims", "normalUskewed", "normalUroot", "normalUright", "normalUradical", "normalUoverdelimiter", "normalUnosuperscript", "normalUnosubscript", "normalUmiddle", "normalUmathunderdelimitervgap", "normalUmathunderdelimiterbgap", "normalUmathunderbarvgap", "normalUmathunderbarrule", "normalUmathunderbarkern", "normalUmathsupsubbottommax", "normalUmathsupshiftup", "normalUmathsupshiftdrop", "normalUmathsupbottommin", "normalUmathsubtopmax", "normalUmathsubsupvgap", "normalUmathsubsupshiftdown", "normalUmathsubshiftdrop", "normalUmathsubshiftdown", "normalUmathstackvgap", "normalUmathstacknumup", "normalUmathstackdenomdown", "normalUmathspaceafterscript", "normalUmathskewedfractionvgap", "normalUmathskewedfractionhgap", "normalUmathrelrelspacing", "normalUmathrelpunctspacing", "normalUmathrelordspacing", "normalUmathrelopspacing", "normalUmathrelopenspacing", "normalUmathrelinnerspacing", "normalUmathrelclosespacing", "normalUmathrelbinspacing", "normalUmathradicalvgap", "normalUmathradicalrule", "normalUmathradicalkern", "normalUmathradicaldegreeraise", "normalUmathradicaldegreebefore", "normalUmathradicaldegreeafter", "normalUmathquad", "normalUmathpunctrelspacing", "normalUmathpunctpunctspacing", "normalUmathpunctordspacing", "normalUmathpunctopspacing", "normalUmathpunctopenspacing", "normalUmathpunctinnerspacing", "normalUmathpunctclosespacing", "normalUmathpunctbinspacing", "normalUmathoverdelimitervgap", "normalUmathoverdelimiterbgap", "normalUmathoverbarvgap", "normalUmathoverbarrule", "normalUmathoverbarkern", "normalUmathordrelspacing", "normalUmathordpunctspacing", "normalUmathordordspacing", "normalUmathordopspacing", "normalUmathordopenspacing", "normalUmathordinnerspacing", "normalUmathordclosespacing", "normalUmathordbinspacing", "normalUmathoprelspacing", "normalUmathoppunctspacing", "normalUmathopordspacing", "normalUmathopopspacing", "normalUmathopopenspacing", "normalUmathopinnerspacing", "normalUmathoperatorsize", "normalUmathopenrelspacing", "normalUmathopenpunctspacing", "normalUmathopenordspacing", "normalUmathopenopspacing", "normalUmathopenopenspacing", "normalUmathopeninnerspacing", "normalUmathopenclosespacing", "normalUmathopenbinspacing", "normalUmathopclosespacing", "normalUmathopbinspacing", "normalUmathnolimitsupfactor", "normalUmathnolimitsubfactor", "normalUmathlimitbelowvgap", "normalUmathlimitbelowkern", "normalUmathlimitbelowbgap", "normalUmathlimitabovevgap", "normalUmathlimitabovekern", "normalUmathlimitabovebgap", "normalUmathinnerrelspacing", "normalUmathinnerpunctspacing", "normalUmathinnerordspacing", "normalUmathinneropspacing", "normalUmathinneropenspacing", "normalUmathinnerinnerspacing", "normalUmathinnerclosespacing", "normalUmathinnerbinspacing", "normalUmathfractionrule", "normalUmathfractionnumvgap", "normalUmathfractionnumup", "normalUmathfractiondenomvgap", "normalUmathfractiondenomdown", "normalUmathfractiondelsize", "normalUmathconnectoroverlapmin", "normalUmathcodenum", "normalUmathcode", "normalUmathcloserelspacing", "normalUmathclosepunctspacing", "normalUmathcloseordspacing", "normalUmathcloseopspacing", "normalUmathcloseopenspacing", "normalUmathcloseinnerspacing", "normalUmathcloseclosespacing", "normalUmathclosebinspacing", "normalUmathcharslot", "normalUmathcharnumdef", "normalUmathcharnum", "normalUmathcharfam", "normalUmathchardef", "normalUmathcharclass", "normalUmathchar", "normalUmathbinrelspacing", "normalUmathbinpunctspacing", "normalUmathbinordspacing", "normalUmathbinopspacing", "normalUmathbinopenspacing", "normalUmathbininnerspacing", "normalUmathbinclosespacing", "normalUmathbinbinspacing", "normalUmathaxis", "normalUmathaccent", "normalUleft", "normalUhextensible", "normalUdelimiterunder", "normalUdelimiterover", "normalUdelimiter", "normalUdelcodenum", "normalUdelcode", "normalUchar", "normalOmegaversion", "normalOmegarevision", "normalOmegaminorversion", "normalAlephversion", "normalAlephrevision", "normalAlephminorversion", "normal ", "nonstopmode", "nonscript", "nolimits", "noligs", "nokerns", "noindent", "nohrule", "noexpand", "noboundary", "noalign", "newlinechar", "mutoglue", "muskipdef", "muskip", "multiply", "muexpr", "mskip", "moveright", "moveleft", "month", "mkern", "middle", "message", "medmuskip", "meaning", "maxdepth", "maxdeadcycles", "mathsurroundskip", "mathsurroundmode", "mathsurround", "mathstyle", "mathscriptsmode", "mathscriptcharmode", "mathscriptboxmode", "mathrulethicknessmode", "mathrulesmode", "mathrulesfam", "mathrel", "mathpunct", "mathpenaltiesmode", "mathord", "mathoption", "mathopen", "mathop", "mathnolimitsmode", "mathitalicsmode", "mathinner", "mathflattenmode", "matheqnogapstep", "mathdisplayskipmode", "mathdirection", "mathdir", "mathdelimitersmode", "mathcode", "mathclose", "mathchoice", "mathchardef", "mathchar", "mathbin", "mathaccent", "marks", "mark", "mag", "luatexversion", "luatexrevision", "luatexbanner", "luafunctioncall", "luafunction", "luaescapestring", "luadef", "luacopyinputnodes", "luabytecodecall", "luabytecode", "lpcode", "lowercase", "lower", "looseness", "long", "localrightbox", "localleftbox", "localinterlinepenalty", "localbrokenpenalty", "lineskiplimit", "lineskip", "linepenalty", "linedirection", "linedir", "limits", "letterspacefont", "letcharcode", "let", "leqno", "leftskip", "leftmarginkern", "lefthyphenmin", "leftghost", "left", "leaders", "lccode", "lateluafunction", "latelua", "lastypos", "lastxpos", "lastskip", "lastsavedimageresourcepages", "lastsavedimageresourceindex", "lastsavedboxresourceindex", "lastpenalty", "lastnodetype", "lastnamedcs", "lastlinefit", "lastkern", "lastbox", "language", "kern", "jobname", "interlinepenalty", "interlinepenalties", "interactionmode", "insertpenalties", "insertht", "insert", "inputlineno", "input", "initcatcodetable", "indent", "immediateassignment", "immediateassigned", "immediate", "ignorespaces", "ignoreligaturesinfont", "ifx", "ifvoid", "ifvmode", "ifvbox", "iftrue", "ifprimitive", "ifpdfprimitive", "ifpdfabsnum", "ifpdfabsdim", "ifodd", "ifnum", "ifmmode", "ifinner", "ifincsname", "ifhmode", "ifhbox", "iffontchar", "iffalse", "ifeof", "ifdim", "ifdefined", "ifcsname", "ifcondition", "ifcat", "ifcase", "ifabsnum", "ifabsdim", "if", "hyphenpenaltymode", "hyphenpenalty", "hyphenchar", "hyphenationmin", "hyphenationbounds", "hyphenation", "ht", "hss", "hskip", "hsize", "hrule", "hpack", "holdinginserts", "hoffset", "hjcode", "hfuzz", "hfilneg", "hfill", "hfil", "hbox", "hbadness", "hangindent", "hangafter", "halign", "gtokspre", "gtoksapp", "gluetomu", "gluestretchorder", "gluestretch", "glueshrinkorder", "glueshrink", "glueexpr", "globaldefs", "global", "gleaders", "gdef", "futurelet", "futureexpandis", "futureexpand", "formatname", "fontname", "fontid", "fontdimen", "fontcharwd", "fontcharic", "fontcharht", "fontchardp", "font", "floatingpenalty", "fixupboxesmode", "firstvalidlanguage", "firstmarks", "firstmark", "finalhyphendemerits", "fi", "fam", "explicithyphenpenalty", "explicitdiscretionary", "expandglyphsinfont", "expandafter", "exhyphenpenalty", "exhyphenchar", "exceptionpenalty", "everyvbox", "everypar", "everymath", "everyjob", "everyhbox", "everyeof", "everydisplay", "everycr", "etokspre", "etoksapp", "escapechar", "errorstopmode", "errorcontextlines", "errmessage", "errhelp", "eqno", "endlocalcontrol", "endlinechar", "endinput", "endgroup", "endcsname", "end", "emergencystretch", "else", "efcode", "edef", "eTeXversion", "eTeXrevision", "eTeXminorversion", "eTeXVersion", "dvivariable", "dvifeedback", "dviextension", "dump", "draftmode", "dp", "doublehyphendemerits", "divide", "displaywidth", "displaywidowpenalty", "displaywidowpenalties", "displaystyle", "displaylimits", "displayindent", "discretionary", "directlua", "dimexpr", "dimendef", "dimen", "detokenize", "delimitershortfall", "delimiterfactor", "delimiter", "delcode", "defaultskewchar", "defaulthyphenchar", "def", "deadcycles", "day", "currentiftype", "currentiflevel", "currentifbranch", "currentgrouptype", "currentgrouplevel", "csstring", "csname", "crcr", "crampedtextstyle", "crampedscriptstyle", "crampedscriptscriptstyle", "crampeddisplaystyle", "cr", "countdef", "count", "copyfont", "copy", "compoundhyphenmode", "clubpenalty", "clubpenalties", "closeout", "closein", "clearmarks", "cleaders", "chardef", "char", "catcodetable", "catcode", "brokenpenalty", "breakafterdirmode", "boxmaxdepth", "boxdirection", "boxdir", "box", "boundary", "botmarks", "botmark", "bodydirection", "bodydir", "binoppenalty", "belowdisplayskip", "belowdisplayshortskip", "begingroup", "begincsname", "batchmode", "baselineskip", "badness", "automatichyphenpenalty", "automatichyphenmode", "automaticdiscretionary", "attributedef", "attribute", "atopwithdelims", "atop", "aligntab", "alignmark", "aftergroup", "afterassignment", "advance", "adjustspacing", "adjdemerits", "accent", "abovewithdelims", "abovedisplayskip", "abovedisplayshortskip", "above", "XeTeXversion", "Uvextensible", "Uunderdelimiter", "Usuperscript", "Usubscript", "Ustopmath", "Ustopdisplaymath", "Ustartmath", "Ustartdisplaymath", "Ustack", "Uskewedwithdelims", "Uskewed", "Uroot", "Uright", "Uradical", "Uoverdelimiter", "Unosuperscript", "Unosubscript", "Umiddle", "Umathunderdelimitervgap", "Umathunderdelimiterbgap", "Umathunderbarvgap", "Umathunderbarrule", "Umathunderbarkern", "Umathsupsubbottommax", "Umathsupshiftup", "Umathsupshiftdrop", "Umathsupbottommin", "Umathsubtopmax", "Umathsubsupvgap", "Umathsubsupshiftdown", "Umathsubshiftdrop", "Umathsubshiftdown", "Umathstackvgap", "Umathstacknumup", "Umathstackdenomdown", "Umathspaceafterscript", "Umathskewedfractionvgap", "Umathskewedfractionhgap", "Umathrelrelspacing", "Umathrelpunctspacing", "Umathrelordspacing", "Umathrelopspacing", "Umathrelopenspacing", "Umathrelinnerspacing", "Umathrelclosespacing", "Umathrelbinspacing", "Umathradicalvgap", "Umathradicalrule", "Umathradicalkern", "Umathradicaldegreeraise", "Umathradicaldegreebefore", "Umathradicaldegreeafter", "Umathquad", "Umathpunctrelspacing", "Umathpunctpunctspacing", "Umathpunctordspacing", "Umathpunctopspacing", "Umathpunctopenspacing", "Umathpunctinnerspacing", "Umathpunctclosespacing", "Umathpunctbinspacing", "Umathoverdelimitervgap", "Umathoverdelimiterbgap", "Umathoverbarvgap", "Umathoverbarrule", "Umathoverbarkern", "Umathordrelspacing", "Umathordpunctspacing", "Umathordordspacing", "Umathordopspacing", "Umathordopenspacing", "Umathordinnerspacing", "Umathordclosespacing", "Umathordbinspacing", "Umathoprelspacing", "Umathoppunctspacing", "Umathopordspacing", "Umathopopspacing", "Umathopopenspacing", "Umathopinnerspacing", "Umathoperatorsize", "Umathopenrelspacing", "Umathopenpunctspacing", "Umathopenordspacing", "Umathopenopspacing", "Umathopenopenspacing", "Umathopeninnerspacing", "Umathopenclosespacing", "Umathopenbinspacing", "Umathopclosespacing", "Umathopbinspacing", "Umathnolimitsupfactor", "Umathnolimitsubfactor", "Umathlimitbelowvgap", "Umathlimitbelowkern", "Umathlimitbelowbgap", "Umathlimitabovevgap", "Umathlimitabovekern", "Umathlimitabovebgap", "Umathinnerrelspacing", "Umathinnerpunctspacing", "Umathinnerordspacing", "Umathinneropspacing", "Umathinneropenspacing", "Umathinnerinnerspacing", "Umathinnerclosespacing", "Umathinnerbinspacing", "Umathfractionrule", "Umathfractionnumvgap", "Umathfractionnumup", "Umathfractiondenomvgap", "Umathfractiondenomdown", "Umathfractiondelsize", "Umathconnectoroverlapmin", "Umathcodenum", "Umathcode", "Umathcloserelspacing", "Umathclosepunctspacing", "Umathcloseordspacing", "Umathcloseopspacing", "Umathcloseopenspacing", "Umathcloseinnerspacing", "Umathcloseclosespacing", "Umathclosebinspacing", "Umathcharslot", "Umathcharnumdef", "Umathcharnum", "Umathcharfam", "Umathchardef", "Umathcharclass", "Umathchar", "Umathbinrelspacing", "Umathbinpunctspacing", "Umathbinordspacing", "Umathbinopspacing", "Umathbinopenspacing", "Umathbininnerspacing", "Umathbinclosespacing", "Umathbinbinspacing", "Umathaxis", "Umathaccent", "Uleft", "Uhextensible", "Udelimiterunder", "Udelimiterover", "Udelimiter", "Udelcodenum", "Udelcode", "Uchar", "Omegaversion", "Omegarevision", "Omegaminorversion", "Alephversion", "Alephrevision", "Alephminorversion" };
				foreach (string command in primitives.Select(x => @"\" + x))
				{
					lang.Commands.Add(new IntelliSense(command) { IntelliSenseType = IntelliSenseType.Command, Token = Token.Primitive });
				}
			}
			if (VM.Default.SuggestFontSwitches)
			{
				string[] mainstyles = new string[] { "rm", "ss", "tt" };
				string[] fontalternatives = new string[] { "tf", "bf", "it", "sl", "bi", "bs", "sc" };
				string[] fontsizemodifiers = new string[] { "xx", "x", "", "a", "b", "c", "d" };
				string[] allfontswitches = mainstyles.Concat(fontalternatives.SelectMany(a => fontsizemodifiers.Select(m => a + m).ToArray()).ToArray()).ToArray();

				foreach (string command in allfontswitches.Select(x => @"\" + x))
				{
					lang.Commands.Add(new IntelliSense(command) { IntelliSenseType = IntelliSenseType.Command, Token = Token.Style });
				}
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

		private void Codewriter_ErrorOccured(object sender, ErrorEventArgs e)
		{
			Exception ex = e.GetException();
			VM.Log($"Editor Error: {ex.Message}\n\t{ex.StackTrace}");
		}

		private void Codewriter_InfoMessage(object sender, string e)
		{
			VM.Log($"Editor Message: {e}");
		}

		private void CkB_IntelliSense_Click(object sender, RoutedEventArgs e)
		{
			PopulateIntelliSense("ConTeXt");
		}

		private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			try
			{
				var ea = ((FrameworkElement)sender).DataContext as EditAction;
				int index = VM.Codewriter.InvertedEditActionHistory.IndexOf(ea);

				UndoCommands.DeselectRange(new(index, (uint)VM.Codewriter.InvertedEditActionHistory.Count));

				UndoCommands.SelectRange(new(0, (uint)(index + 1)));
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
		}

		private void Codewriter_TextChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			VM.UpdateOutline(((CodeWriter)sender).Lines.ToList(), true);
		}

		private void UndoCommands_ItemClick(object sender, ItemClickEventArgs e)
		{
			try
			{
				VM.Codewriter.TextAction_Undo(e.ClickedItem as EditAction);
				if (VM.Codewriter.EditActionHistory.Count == 0)
				{
					UndoFlyout.Hide();
				}
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
		}

		private void UndoCommands_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			UndoCommands.DeselectRange(new(0, (uint)VM.Codewriter.InvertedEditActionHistory.Count));
		}

		private void Fly_Color_Closing(object sender, FlyoutBaseClosingEventArgs e)
		{
			VM.Default.SaveSettings();
		}
	}
}
