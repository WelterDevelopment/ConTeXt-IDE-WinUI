﻿using CodeEditorControl_WinUI;
using CommunityToolkit.WinUI.Controls;
using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using ConTeXt_IDE.Shared.Helpers;
using ConTeXt_IDE.Shared.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Input;
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
using PDFjs.WinUI;
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
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using static ConTeXt_IDE.Shared.SystemBackdropWindow;
using AppWindow = Windows.UI.WindowManagement.AppWindow;

namespace ConTeXt_IDE
{
	public sealed partial class MainPage : Page
	{

		private Color CW_BackgroundColor = Color.FromArgb(25, 135, 135, 135);
		private SolidColorBrush CW_Background = new(Color.FromArgb(25, 135, 135, 135));
		private ViewModel VM { get; } = App.VM;
		public MainPage()
		{
			try
			{
				InitializeComponent();
				VM.MainPage = this;
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

				MenuSave.Command = new RelayCommand(() => { Btn_Save_Click(null, null); });
				MenuSave.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control });

				MenuCompile.Command = new RelayCommand(() => { Btn_Compile_Click(null, null); });
				MenuCompile.KeyboardAccelerators.Add(new() { Key = VirtualKey.Enter, Modifiers = VirtualKeyModifiers.Control });
				MenuCompile.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding() { Path = new("CurrentFileItem.IsTexFile") });

				MenuSyncTeX.Command = new RelayCommand(() => { FindInPDF(); });
				MenuSyncTeX.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Space, Modifiers = VirtualKeyModifiers.Control });
				MenuSyncTeX.SetBinding(MenuFlyoutItem.IsEnabledProperty, new Binding() { Path = new("CurrentFileItem.IsTexFile") });
				MenuSyncTeX.SetBinding(MenuFlyoutItem.VisibilityProperty, new Binding() { Path = new("CurrentProject.UseSyncTeX") });
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
			//Tbv_Start.AddHandler(PointerReleasedEvent, new PointerEventHandler(Tbv_PointerReleased), true);
		}

		public async void FirstStart()
		{

			if (VM.Default.FirstStart)
			{
				ShowTeachingTip("AddProject", Btn_Addproject);
				VM.Default.FirstStart = false;
			}
			string version = "";
			await Task.Run(async () =>
			{
				version = await GetVersion();
			});
			VM.Default.ContextVersion = version;
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
				var p = Path.Combine(Package.Current.Installed­Location.Path, @"ConTeXt-IDE.Desktop", @"About.md");
				StorageFile storageFile;

				if (File.Exists(p))
				{
					storageFile = await StorageFile.GetFileFromPathAsync(p);
				}
				else
				{
					storageFile = await StorageFile.GetFileFromApplicationUriAsync(new("ms-appx:///About.md"));
				}

			(sender as TextBlock).Text = await File.ReadAllTextAsync(storageFile.Path);
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
		}

		private MenuFlyoutItem MenuSave = new MenuFlyoutItem() { Text = "Save", Icon = new SymbolIcon() { Symbol = Symbol.Save } };
		private MenuFlyoutItem MenuCompile = new MenuFlyoutItem() { Text = "Compile", Icon = new SymbolIcon() { Symbol = Symbol.Play } };
		private MenuFlyoutItem MenuSyncTeX = new MenuFlyoutItem() { Text = "Find in PDF", Icon = new FontIcon() { Glyph = "\xEBE7" } };

		private async void Codewriter_Loaded(object sender, RoutedEventArgs e)
		{
			CodeWriter cw = sender as CodeWriter;

			VM.Codewriter = cw;

			if (!cw.ContextMenu.Items.Any(x => { if (x is MenuFlyoutItem item) return item.Text == "Save"; else return false; }))
				cw.Action_Add(MenuSave);

			if (!cw.ContextMenu.Items.Any(x => { if (x is MenuFlyoutItem item) return item.Text == "Compile"; else return false; }) && cw.Language.Name == "ConTeXt")
				cw.Action_Add(MenuCompile);

			if (VM.CurrentProject != null)
				if (!cw.ContextMenu.Items.Any(x => { if (x is MenuFlyoutItem item) return item.Text == "Find in PDF"; else return false; }) && VM.CurrentProject.UseSyncTeX && cw.Language.Name == "ConTeXt")
					cw.Action_Add(MenuSyncTeX);

			if (VM.CurrentFileItem.IsTexFile)
				cw.UpdateSuggestions();

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
			string filePath = Path.Combine(storageFolder.Path, file);
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
					VM.IsLoadingBarVisible = false;
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

			VM.Startup();
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
						//VM.AddFileItemAplphabetically(fileItem.Children, fi);
						//	fileItem.Children.Add(fi);
					}
					else
						VM.InfoMessage("Error", "The file " + name + " does already exist.", InfoBarSeverity.Error);
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

						//	VM.AddFileItemAplphabetically(fileItem.Children,fi);
						//fileItem.Children.Add(fi);
					}
					else
					{
						VM.InfoMessage("Error", "The folder " + name + " does already exist.", InfoBarSeverity.Error);
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
			NewFile((sender as FrameworkElement).DataContext as FileItem);
		}

		private async void AddFolderRoot_Click(object sender, RoutedEventArgs e)
		{
			NewFolder((sender as FrameworkElement).DataContext as FileItem);
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
				RemoveItem(VM.CurrentProject?.Directory, fi);
				VM.CloseRequested = true;
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
				//if (fi.File is StorageFile sf)
				//	fi.FileLanguage = FileItem.GetFileType(sf.FileType);
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
				//VM.Log($"Renaming {type.ToString().ToLower()} {startstring} to {tb.Text}");
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

		private void Tbv_FileItems_DragStarting(object sender, DragStartingEventArgs e)
		{
			VM.IsDragging = true;
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

		private void PagePointerPressed(object sender, PointerRoutedEventArgs e)
		{
			VM.IsUsingTouch = e.Pointer.PointerDeviceType == PointerDeviceType.Touch | e.Pointer.PointerDeviceType == PointerDeviceType.Pen;
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
					//var tip = new TeachingTip() { XamlRoot = XamlRoot, Title = fileitem.FileName, PreferredPlacement = TeachingTipPlacementMode.Center, IsLightDismissEnabled = true };
					Ttp.Title = fileitem.FileName;
					Ttp.PreferredPlacement = TeachingTipPlacementMode.RightTop;
					Ttp.Target = treeviewitem; // (FrameworkElement)e.OriginalSource;
					Ttp.IsLightDismissEnabled = true;
					Ttp.Content = null;
					var content = new Grid() { Background = new SolidColorBrush(Colors.LightGray) };
					content.Children.Add(new Image() { Source = await LoadImage((StorageFile)fileitem.File, type), });
					Ttp.HeroContent = content;
					Ttp.HeroContentPlacement = TeachingTipHeroContentPlacementMode.Bottom;
					//RootGrid.Children.Add(tip);


					Ttp.IsOpen = true;
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


		private void Tbv_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// ToDo: This workaround to collapse the currently selected ribbon item is the hackiest of hacks, needs better logic
			FrameworkElement item = sender as FrameworkElement;

			if (string.Compare((MainRibbon.SelectedItem as TabViewItem)?.Tag as string, item?.Tag as string) == 0)
			{
				MainRibbon.SelectedIndex = -1;
				e.Handled = true;
			}

		}
		private static async Task<ImageSource> LoadImage(StorageFile file, string type)
		{
			ImageSource image = null;
			FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);
			switch (type)
			{
				case ".svg": SvgImageSource svgsource = new(); svgsource.RasterizePixelWidth = 400; svgsource.SetSourceAsync(stream); image = svgsource; break;
				default: BitmapImage bmpsource = new(); bmpsource.SetSourceAsync(stream); image = bmpsource; break;
			}


			return image;
		}

		private void SetRoot_Click(object sender, RoutedEventArgs e)
		{
			var ei = (FileItem)(sender as FrameworkElement).DataContext;
			if (true) //ei.Level == 0
			{
				//ei.IsRoot = true;
				VM.CurrentRootItem = ei;
				VM.CurrentProject.RootFilePath = ei.File.Path;
			}
			else
			{
				VM.InfoMessage("Warning", "The main file must be in the root folder of the project", InfoBarSeverity.Warning);
			}
		}

		#endregion

		#region Compiler Operations

		AppWindow PDFWindow;
		public async void CompileTex(bool compileRoot = false, FileItem fileToCompile = null)
		{
			if (!VM.IsSaving)
				try
				{
					if (!VM.IsInstalling)
					{
						VM.IsIndeterminate = true;
						TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Indeterminate);
					}
					VM.IsTeXError = false;
					VM.IsPaused = false;
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
						filetocompile = VM.CurrentRootItem ?? VM.CurrentFileItem;
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

						if (error == null)
						{
							if (!VM.IsInstalling)
							{
								TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.NoProgress);
								if (App.MainWindow.AW.ClientSize.Height == 0) // Dirty hack to check if window is minimized
									FlashWindow.Flash(App.MainWindow.hWnd, FlashWindow.FLASHW_TRAY, 0);
							}

						}

						if (error != null)
						{
							StorageFile errorfile = error as StorageFile;
							string text = await FileIO.ReadTextAsync(errorfile);

							VM.IsTeXError = true;
							if (!VM.IsInstalling)
							{
								TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Paused);
								TaskbarUtility.SetProgressValue(100, 100);
							}

							// BAD code, quick hack to convert the lua table to a json format. Depending on special characters in the error message, the JsonConvert.DeserializeObject function can throw errors.
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
									Codewriter.SelectLine(new(0, VM.ConTeXtErrorMessage.linenumber - 1));
									Codewriter.CenterView();
								}
								catch { }
							}
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
												VM.Log($"Opening {syncTeX.FileName}");
											}
										}
									}
								}
								else
								{
									if (VM.Default.CurrentPDFViewer.Name == "Default")
										await Launcher.LaunchFileAsync(pdfout);
									else
									{
										OpenPDFInExternalViewer(currFolder.Path, VM.Default.CurrentPDFViewer.Path, pdfout.Name);
									}
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
					Codewriter.Focus(FocusState.Keyboard);
				}
				catch (Exception f)
				{
					VM.Log("Exception at compile: " + f.Message);
				}
			VM.IsSaving = false;
		}

		StorageFile lastPDFFile = null;

		public async Task<bool> OpenPDF(StorageFile pdfout = null, bool openpdfwindow = false)
		{
			try
			{
				StorageFile pdffile = pdfout ?? lastPDFFile;
				if (pdffile != null){
					lastPDFFile = pdffile;
					if (VM.Default.PDFWindow & !VM.IsInternalViewerActive)
					{
						if (pDFWindow == null)
						{
							pDFWindow = new();
							pDFWindow.Activate();
							if (VM.CurrentProject.UseSyncTeX)
								pDFWindow.PDFReader.SyncTeXRequested += PDFjsViewer_SyncTeXRequested;
							pDFWindow.AW.Closing += async (a, b) =>
							{
								await PDFReader.OpenPDF(pdffile);
								VM.IsInternalViewerActive = true;
								pDFWindow = null;
							};
						}
						pDFWindow?.PDFReader.OpenPDF(pdffile);
					}
					else
					{
						VM.IsInternalViewerActive = true;
						await PDFReader.OpenPDF(pdffile);
					}
				}
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
			Codewriter.Save();
			CompileTex(false, fi);
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{

			await VM.Save((sender as FrameworkElement).DataContext as FileItem);
			Codewriter.Save();
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
			else if (Directory.Exists(VM.Default.ContextDistributionPath + @"\tex\texmf-windows-arm64"))
			{
				return @"\texmf-windows-arm64";
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
			info.Arguments = " " + param + "\"" + texFileName + "\"";

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

		public void OpenPDFInExternalViewer(string workingDirectory, string exepath, string pdfFileName, string param = "")
		{
			try
			{
				Process p = new Process();
				ProcessStartInfo info = new ProcessStartInfo()
				{
					RedirectStandardInput = false,
					RedirectStandardOutput = false,
					RedirectStandardError = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					UseShellExecute = false,
					WorkingDirectory = workingDirectory
				};

				p.StartInfo = info;
				info.FileName = Environment.ExpandEnvironmentVariables(exepath);
				info.Arguments = " " + param + " " + pdfFileName;

				p.Exited += (a, b) => { p.Close(); };

				p.Start();
			}
			catch
			{
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
						await VM.Save(fi);
					}
				}
				if (VM.FileItems.Contains(fi))
				{
					VM.CloseRequested = true;
					VM.FileItems.Remove(fi);
					fi.IsOpened = false;
				}

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
				var helpItem = VM.Default.HelpItemList.FirstOrDefault(x => x.ID == ID);
				if (helpItem != null && !helpItem.Shown)
				{
					//var tip = new TeachingTip() { XamlRoot = XamlRoot, Title = helpItem.Title, PreferredPlacement = TeachingTipPlacementMode.Center, IsLightDismissEnabled = false, IsOpen = false };
					Ttp.Target = (FrameworkElement)Target;
					Ttp.Title = helpItem.Title;
					Ttp.IsLightDismissEnabled = false;
					Ttp.PreferredPlacement = TeachingTipPlacementMode.RightBottom;
					if (!string.IsNullOrWhiteSpace(helpItem.Subtitle))
						Ttp.Subtitle = helpItem.Subtitle;
					Ttp.Content = new TextBlock() { TextWrapping = TextWrapping.WrapWholeWords, Text = helpItem.Text };
					//Ttp.Content = helpItem.Text;
					//RootGrid.Children.Add(Ttp);
					//Ttp_Tbk.Text = helpItem.Text;
					Ttp.IsOpen = true;
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
						LogGridSplitter.Height = new GridLength(1, GridUnitType.Auto);
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
						ProjectsGridSplitter.Height = new GridLength(1, GridUnitType.Auto);
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
						ContentGridSplitter.Width = new GridLength(1, GridUnitType.Auto);
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
						PDFGridSplitter.Width = new GridLength(1, GridUnitType.Auto);
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

									foreach (StorageFile file in await proj.Folder.GetFilesAsync())
									{
										if (file.Name.StartsWith("prd_") && file.FileType == ".tex")
										{
											proj.RootFilePath = file.Path;
											break;
										}
									}

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
											case "markdown": rootfile = "prd_markdown.tex"; break;
											case "cv": rootfile = "prd_cv.tex"; break;
											case "projpres":
												rootfile = "prd_presentation.tex";
												proj.Modes.FirstOrDefault(x => x.Name == "screen").IsSelected = true;
												proj.Modes.Add(new() { Name = "handout" });
												break;
											case "projthes": rootfile = "prd_thesis.tex"; break;
											case "single": rootfile = "prd_main.tex"; break;
											default: break;
										}

										proj.RootFilePath = Path.Combine(folder.Path, rootfile);


										if (VM.Default.ProjectList.Any(x => x.Name == proj.Name))
										{
											VM.Default.ProjectList.Remove(VM.Default.ProjectList.First(x => x.Folder.Path == proj.Folder.Path));
										}

										VM.Default.ProjectList.Add(proj);
										VM.CurrentProject = proj;
										if (project == "markdown")
											VM.OpenFile("prd_markdown.md");

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

			App.MainWindow.RequestedTheme = (ElementTheme)Enum.Parse(typeof(ElementTheme), VM.Default.Theme);
			SetColor(VM.AccentColor, (ElementTheme)Enum.Parse(typeof(ElementTheme), VM.Default.Theme), true);
			//VM.InfoMessage("You changed the theme", "Not every UI element updates its theme at runtime. You may want to restart the app.");
			await Task.Delay(50);
			PDFReader.Theme = VM.Default.Theme;
			if (pDFWindow != null) {
				pDFWindow.RequestedTheme = (ElementTheme)Enum.Parse(typeof(ElementTheme), VM.Default.Theme);
				pDFWindow.PDFReader.Theme = VM.Default.Theme;
				pDFWindow.ResetTitleBar();
					}
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

				if (proj == VM.CurrentProject)
				{
					VM.FileItems.Clear();
					VM.Default.ProjectList.Remove(proj);
					VM.CurrentProject = VM.Default.ProjectList.FirstOrDefault();
				}
				else
				{
					VM.Default.ProjectList.Remove(proj);
				}

				VM.PopulateJumpList();
				VM.Log($"Project {proj.Name} removed");

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
				VM.IsInternalViewerActive = false;
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
				VM.IsError = false;
				VM.IsTeXError = false;
				VM.IsInstalling = true;
				VM.IsIndeterminate = true;
				TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Indeterminate);
				VM.InfoMessage("Updating", "Your ConTeXt distribution is getting updated. This can take up to 15 minutes. Do not abort this process!", InfoBarSeverity.Informational);

				bool UpdateSuccessful = false;
				bool temporarilyshowlog = !VM.Default.ShowLog;

				if (temporarilyshowlog)
					VM.Default.ShowLog = true;
				await Task.Run(async () => { UpdateSuccessful = await Update(); });

				string version = "";
				await Task.Run(async () => { version = await GetVersion(); });
				VM.Default.ContextVersion = version;

				if (temporarilyshowlog)
					VM.Default.ShowLog = false;

				if (UpdateSuccessful)
				{
					VM.InfoMessage("Update complete!", "Your ConTeXt distribution is up n' running!", InfoBarSeverity.Success);
					TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.NoProgress);
				}
				else
				{
					VM.InfoMessage("Error", "Something went wrong. Please try again later.", InfoBarSeverity.Error);
					TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Error);
				}
			}
			else
			{
				VM.InfoMessage("No internet connection", "You need to be connected to the internet in order to update your ConTeXt distribution!", InfoBarSeverity.Warning);
				TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Paused);
			}

			VM.IsInstalling = false;
			VM.IsIndeterminate = false;
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
								VM.IsIndeterminate = false;
								VM.ProgressValue = 0;
								//TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Normal);
								//TaskbarUtility.SetProgressValue(3,100);
							}
							else if (mp.Success)
							{
								int percentage = 0;
								if (int.TryParse(mp.Groups[1]?.Value, out percentage))
								{
									if (percentage <= 100 && percentage >= 0)
									{
										VM.IsIndeterminate = false;
										double val = (double)Math.Max(0.5d, (double)percentage);
										VM.ProgressValue = val;
										TaskbarUtility.SetProgressValue(Math.Max(3, (int)val), 100);
									}
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

		private async Task<string> GetVersion()
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

				info.FileName = VM.Default.ContextDistributionPath + @"\tex" + getversion() + @"\bin\context.exe";// @"C:\Windows\System32\cmd.exe";
				info.Arguments = "--version";
				//sw.WriteLine("setx path \"%PATH%;" + VM.Default.ContextDistributionPath + @"\tex\texmf-mswin\bin" + "\"");
				string version = "";

				p.OutputDataReceived += (a, b) =>
				{
					//DispatcherQueue.TryEnqueue(async () =>
					//{
					if (b?.Data != null)
					{
						Match mu = Regex.Match(b.Data, @"(current version: *?)(\d{2,4}\.\d{2}\.\d{2} *?\d{2}\:\d{2})");
						if (mu.Success)
						{
							version = mu.Groups[2].Value;
						}
					}
					//});

				};

				p.Start();
				p.BeginOutputReadLine();

				p.WaitForExit();
				p.Close();

				return version;
			}
			catch
			{
				return "";
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

		private async void Btn_Compile_Click(object sender, RoutedEventArgs e)
		{
			await VM.SaveAll();
			Codewriter.Save();
			CompileTex();
		}

		private async void Btn_CompileRoot_Click(object sender, RoutedEventArgs e)
		{
			await VM.SaveAll();
			Codewriter.Save();
			CompileTex(true);
		}

		PDFWindowViewer pDFWindow { get; set; }
		private void Undo_Click(object sender, SplitButtonClickEventArgs e)
		{
			Codewriter.TextAction_Undo();
			Codewriter.Focus(FocusState.Keyboard);
		}

		private async void Btn_Save_Click(object sender, RoutedEventArgs e)
		{
			
			await VM.Save();
			Codewriter.Save();
		}

		private async void Btn_SaveAll_Click(object sender, RoutedEventArgs e)
		{
			await VM.SaveAll();
			Codewriter.Save();
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
				Settings.Default = Settings.RestoreSettings();
				Unload_Click(null, null);
				VM.ShowCommandReference = false;
				VM.PopulateJumpList();
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

		private async void Btn_AddPDFViewer_Click(object sender, RoutedEventArgs e)
		{
			PDFViewer viewer = new PDFViewer();
			string name = "";
			var cd = new ContentDialog() { XamlRoot = XamlRoot, Title = "Add external PDF viewer", PrimaryButtonText = "Ok", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };
			TextBox tbn = new TextBox() { Header = "Name" };
			TextBox tbp = new TextBox() { Header = "Executable" };
			TextBlock tbph = new() { Text = @"You can simply enter the name of your viewer (e.g. 'sumatrapdf'), but only if it is added to your PATH environment variable! If not, you need to enter the full path to the .exe file, like '%USERPROFILE%\AppData\Local\SumatraPDF\SumatraPDF.exe'" };
			StackPanel stk = new();
			stk.Children.Add(tbn);
			stk.Children.Add(tbp);
			stk.Children.Add(tbph);
			cd.Content = stk;
			if (await cd.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrEmpty(tbn.Text) && !string.IsNullOrEmpty(tbp.Text))
			{
				viewer.Name = tbn.Text;
				viewer.Path = tbp.Text;
				VM.Default.PDFViewerList.Add(viewer);
				VM.Default.CurrentPDFViewer = viewer;
				VM.Default.SaveSettings();
			}
		}

		private void Fly_PDFViewer_Closing(object sender, FlyoutBaseClosingEventArgs e)
		{
			VM.Default.SaveSettings();
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
				{
					if (VM.Default.CurrentPDFViewer.Name == "Default")
						await Launcher.LaunchFileAsync(sf);
					else
					{
						OpenPDFInExternalViewer(lsf.Path + hf.Path, VM.Default.CurrentPDFViewer.Path, sf.Name);
					}
				}

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
			NumberBox nbx = sender as NumberBox;
			var point = e.GetCurrentPoint(sender as UIElement);
			int wheeldelta = point.Properties.MouseWheelDelta;
			if (wheeldelta > 0)
			{
				if (VM.Default.FontSize < nbx.Maximum)
					VM.Default.FontSize++;
			}
			else if (VM.Default.FontSize > nbx.Minimum)
			{
				VM.Default.FontSize--;
			}
			e.Handled = true;
		}
		private void Tbx_TabLength_Wheel(object sender, PointerRoutedEventArgs e)
		{
			NumberBox nbx = sender as NumberBox;
			var point = e.GetCurrentPoint(sender as UIElement);
			int wheeldelta = point.Properties.MouseWheelDelta;
			if (wheeldelta > 0)
			{
				if (VM.Default.TabLength < nbx.Maximum)
					VM.Default.TabLength++;
			}
			else if (VM.Default.TabLength > nbx.Minimum)
			{
				VM.Default.TabLength--;
			}
			e.Handled = true;
		}

		private void Nbx_RibbonMarginValue_Wheel(object sender, PointerRoutedEventArgs e)
		{
			NumberBox nbx = sender as NumberBox;
			var point = e.GetCurrentPoint(sender as UIElement);
			int wheeldelta = point.Properties.MouseWheelDelta;
			if (wheeldelta > 0)
			{
				if (VM.Default.RibbonMarginValue < nbx.Maximum)
					VM.Default.RibbonMarginValue++;
			}
			else if (VM.Default.RibbonMarginValue > nbx.Minimum)
			{
				VM.Default.RibbonMarginValue--;
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
				setting.Backdrop = VM.Default.Backdrop;
				setting.AccentColor = accentColor.Color;

				//setting.AccentColorLowLow = Colors.Transparent;

				App.MainWindow.SetBackdrop((BackdropType)Enum.Parse(typeof(BackdropType), VM.Default.Backdrop));

				Application.Current.Resources["SystemAccentColor"] = accentColor.Color;
				Application.Current.Resources["SystemAccentColorLight2"] = setting.AccentColorLow;
				Application.Current.Resources["SystemAccentColorDark1"] = setting.AccentColorLow.ChangeColorBrightness(0.1f);

			}

			// App.Current.Resources.MergedDictionaries.Add(palette);

			if (App.MainWindow.IsCustomizationSupported)
			{
				App.MainWindow.AW.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
				App.MainWindow.AW.TitleBar.ButtonBackgroundColor = Colors.Transparent;
				App.MainWindow.AW.TitleBar.ButtonHoverBackgroundColor = VM.Default.Backdrop == "Mica" ? Color.FromArgb(50, 125, 125, 125) : setting.AccentColor;
				App.MainWindow.AW.TitleBar.ButtonHoverForegroundColor = setting.ActualTheme == ApplicationTheme.Light ? Colors.Black : Colors.White;
				App.MainWindow.AW.TitleBar.ButtonForegroundColor = theme == ElementTheme.Light ? Colors.Black : Colors.White;
				App.MainWindow.AW.TitleBar.ButtonInactiveForegroundColor = theme == ElementTheme.Light ? Color.FromArgb(255, 50, 50, 50) : Color.FromArgb(255, 200, 200, 200);
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
			{
				DownloadModule(module);
				VM.ContextModules.Where(x => x.Name == module.Name).FirstOrDefault().IsInstalled = true;
				VM.Default.InstalledContextModules = VM.ContextModules.Where(x => x.IsInstalled).Select(x => x.Name).ToList();
			}
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
				VM.ContextModules.Where(x => x.Name == module.Name).FirstOrDefault().IsInstalled = false;
				VM.Default.InstalledContextModules = VM.ContextModules.Where(x => x.IsInstalled).Select(x => x.Name).ToList();
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
				VM.IsInstalling = true;
				TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Indeterminate);

				VM.InfoMessage($"Downloading Module {module.Name}", "Please wait...");
				using (WebClient wc = new WebClient())
				{
					string filepath = Path.Combine(ApplicationData.Current.LocalFolder.Path, module.Name + ".zip");

					if (File.Exists(filepath))
					{
						File.Delete(filepath);
					}

					bool success = false;
					wc.DownloadProgressChanged += (a, b) =>
					{
						VM.ProgressValue = b.ProgressPercentage;
						VM.InfoText = VM.ProgressValue.ToString() + "%";
						TaskbarUtility.SetProgressValue(b.ProgressPercentage, 100);
					};

					wc.DownloadFileCompleted += async (a, b) =>
					{
						VM.IsIndeterminate = true;
						TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.NoProgress);
						TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Indeterminate);
						VM.InfoMessage($"Installing Module {module.Name}", "Please wait...");
						success = await InstallModule(module, filepath);

						VM.IsInstalling = false;

						module.IsInstalled = true;

						if (success)
						{
							VM.InfoMessage("Success", $"Module {module.Name} has been successfully installed.", InfoBarSeverity.Success);
						}
						else
						{
							VM.InfoMessage("Error", $"Module {module.Name} could not be installed.", InfoBarSeverity.Error);
						}
						TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.NoProgress);
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
				await Task.Run(() =>
				{

					string xml = File.ReadAllText(Path.Combine(ApplicationData.Current.LocalFolder.Path, @"tex\texmf-context\tex\context\interface\mkiv\context-en.xml"));
					using (StringReader reader = new StringReader(xml))
					{

						Interface contextinterface = (Interface)serializer.Deserialize(reader);
						List<Command> commands = contextinterface.InterfaceList.SelectMany(x => x.Command).ToList();

						List<string> commandnames = new();

						foreach (Command command in commands)
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
									if (argument is Assignments assignments)
									{
										if (assignments?.Parameter != null)
											foreach (var param in assignments?.Parameter)
												if (param?.Resolve != null)
												{
													if (param.Constant == null)
														param.Constant = new();
													foreach (var res in param?.Resolve)
														if (res != null)
															param.Constant.Add(new() { Type = res.Name });
												}
										if (assignments?.Inherit != null)
											foreach (var inherit in assignments?.Inherit)
												if (inherit?.Name != null)
												{

													string inheritedcommandname = inherit.Name;
													Command inheritedcommand = commands.FirstOrDefault(x => x.Name == inheritedcommandname);
													if (inheritedcommand != null)
													{
														Assignments inheritasmt = (Assignments)inheritedcommand?.Arguments?.ArgumentsList?.FirstOrDefault(x => x is Assignments asmt && asmt?.List == "yes");
														if (inheritasmt?.Parameter != null)
														{
															//assignments.Parameter = new();
															assignments.Parameter.Add(new() { Name = $"Inherited from \\{inheritedcommandname}:", Constant = null });
															foreach (var param in inheritasmt?.Parameter)
																assignments.Parameter.Add(param);
															//VM.Log(string.Join(", ", assignments.Parameter.Select(x => x.Name)));
														}
													}
												}
										if (assignments?.Parameter != null)
											foreach (var parameter in assignments?.Parameter)
											{
												if (parameter?.Inherit != null)
													foreach (var inh in parameter?.Inherit)
													{
														string inheritedcommandname = inh.Name;
														Command inheritedcommand = commands.FirstOrDefault(x => x.Name == inheritedcommandname);
														if (inheritedcommand != null)
														{
															//if (parameter.Constant == null)
															parameter.Constant = new();
															Keywords inheritkeyw = (Keywords)inheritedcommand?.Arguments?.ArgumentsList?.FirstOrDefault(x => x is Keywords keyw && keyw?.List == "yes");
															if (inheritkeyw?.Constant != null)
															{
																parameter.Constant.Add(new() { Type = $"\\{inheritedcommandname}->" });
																foreach (var cons in inheritkeyw?.Constant)
																	parameter.Constant.Add(cons);
																//VM.Log(string.Join(", ", assignments.Parameter.Select(x => x.Name)));
															}
															//if (inheritkeyw?.Inherit != null)
															//{
															//	string ininheritedcommandname = inheritkeyw?.Inherit?.Name;
															//	Command ininheritedcommand = commands.FirstOrDefault(x => x.Name == ininheritedcommandname);
															//	Keywords ininheritkeyw = (Keywords)ininheritedcommand?.Arguments?.ArgumentsList?.FirstOrDefault(x => x is Keywords keyw && keyw?.List == "yes");
															//	parameter.Constant.Add(new() { Type = $"\\{ininheritedcommandname}->" });
															//	foreach (var cons in ininheritkeyw?.Constant)
															//		parameter.Constant.Add(cons);
															//	//VM.Log(string.Join(", ", assignments.Parameter.Select(x => x.Name)));
															//}
														}
													}
											}
									}
									if (argument is Keywords keywords)
									{
										if (keywords?.Inherit != null)
											if (keywords?.Inherit?.Name != null)
											{
												string inheritedcommandname = keywords?.Inherit.Name;
												Command inheritedcommand = commands.FirstOrDefault(x => x.Name == inheritedcommandname);
												if (inheritedcommand != null)
												{
													Keywords inheritkeyw = (Keywords)inheritedcommand?.Arguments?.ArgumentsList?.FirstOrDefault(x => x is Keywords keyw && keyw?.List == "yes");
													if (inheritkeyw?.Constant != null)
													{
														keywords.Constant.Add(new() { Type = $"\\{inheritedcommandname}->" });
														foreach (var param in inheritkeyw?.Constant)
															keywords.Constant.Add(param);
													}
												}
											}
									}
								}
							}
						}

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
									Command newcommand = (Command)instancecommand.DeepClone();
									newcommand.Name = sequence + constant.Value;
									commands.Insert(commands.IndexOf(instancecommand), newcommand);
								}
								commands.Remove(instancecommand);
							}
						}

						foreach (var command in commands)
						{
							int count = commandnames.Count(x => x == command.Name);
							command.ID = count;
							commandnames.Add(command.Name);
							command.IsFavorite = VM.Default.CommandFavorites.Any(x => x.Name == @"\" + (command.Type == "environment" ? "start" : "") + command.Name && x.ID == command.ID);
						}

						contextcommands = commands;



						IOrderedEnumerable<IGrouping<string, Command>> query = from item in contextcommands
																																																													group item by item?.Category into g
																																																													where !string.IsNullOrWhiteSpace(g.Key)
																																																													orderby g.Key
																																																													select g;

						DispatcherQueue.TryEnqueue(() =>
						{
							VM.ContextCommandGroupList = query.SelectMany(x => x).Select(x => x.Category).Distinct().ToList();
							if (VM.Default.CommandGroups.Count == 0)
							{
								VM.Default.CommandGroups = VM.ContextCommandGroupList.Select(x => new CommandGroup() { Name = x, IsSelected = !disableGroups.Contains(x) }).ToList();
							}
						});


					}

				});

				IOrderedEnumerable<IGrouping<string, Command>> filtered = null;
				string text = Searchtext.Text;
				await Task.Run(() => { filtered = UpdateSearchFilter(text, VM.Default.FilterFavorites); });

				VM.ContextCommandGroupList = filtered.SelectMany(x => x).Select(x => x.Category).Distinct().ToList();
				cvs.Source = filtered;


				//DocumentationView.SelectedIndex = -1;


				PopulateIntelliSense("ConTeXt");

			}
			catch (Exception ex)
			{
				VM.InfoMessage("IntelliSense initialization error", ex.Message, InfoBarSeverity.Error);
			}

		}

		private string ConvertCDName(string value)
		{
			if (value is string parametertype && value != null)
			{
				if (parametertype.StartsWith("cd:"))
				{
					string type = parametertype.Remove(0, 3);
					return type.ToUpper();
				}
				else return parametertype;
			}
			else return value;
		}

		private void PopulateIntelliSense(string name)
		{
			try
			{
				var lang = FileLanguages.LanguageList.First(x => x.Name == name);
				Task.Run(() =>
				{

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
											Name = assignments?.Inherit?.FirstOrDefault()?.Name ?? "",
											Parameters = assignments?.Parameter?.Select(x => new CodeEditorControl_WinUI.KeyValue() { Name = x.Name, Description = "Type: " + x.Constant?.FirstOrDefault()?.Type, Values = x.Constant?.Select(x => ConvertCDName(x.Type)).ToList() } as CodeEditorControl_WinUI.Parameter).ToList(),
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
						string primitives = "year|xtokspre|xtoksapp|xtoks|xspaceskip|xleaders|xdefcsname|xdef|wrapuppar|wordboundary|widowpenalty|widowpenalties|wd|vtop|vss|vsplit|vskip|vsize|vrule|vpack|vfuzz|vfilneg|vfill|vfil|vcenter|vbox|vbadness|valign|vadjust|uppercase|unvpack|unvcopy|unvbox|untraced|unskip|unpenalty|unletprotected|unletfrozen|unless|unkern|unhpack|unhcopy|unhbox|unexpandedloop|underline|undent|uchyph|uccode|tracingstats|tracingrestores|tracingparagraphs|tracingpages|tracingoutput|tracingonline|tracingnodes|tracingnesting|tracingmath|tracingmarks|tracingmacros|tracinglostchars|tracinglevels|tracinginserts|tracingifs|tracinghyphenation|tracinggroups|tracingfullboxes|tracingfonts|tracingexpressions|tracingcommands|tracingassigns|tracingalignments|tracingadjusts|tpack|toscaled|topskip|topmarks|topmark|tomathstyle|tolerant|tolerance|tokspre|toksdef|toksapp|toks|tokenized|tointeger|todimension|time|thinmuskip|thickmuskip|thewithoutunit|the|textstyle|textfont|textdirection|tabskip|tabsize|swapcsvalues|supmarkmode|string|splittopskip|splitmaxdepth|splitfirstmarks|splitfirstmark|splitdiscards|splitbotmarks|splitbotmark|span|spaceskip|spacefactor|snapshotpar|skipdef|skip|skewchar|showtokens|showthe|shownodedetails|showlists|showifs|showgroups|showboxdepth|showboxbreadth|showbox|show|shipout|shapingpenalty|shapingpenaltiesmode|sfcode|setlanguage|setfontid|setbox|semiprotected|semiexpanded|scrollmode|scriptstyle|scriptspace|scriptscriptstyle|scriptscriptfont|scriptfont|scantokens|scantextokens|scaledfontdimen|savingvdiscards|savinghyphcodes|savecatcodetable|rpcode|romannumeral|rightskip|rightmarginkern|righthyphenmin|right|retokenized|relpenalty|relax|raise|radical|quitvmode|quitloop|pxdimen|protrusionboundary|protrudechars|protected|prevgraf|prevdepth|pretolerance|prerelpenalty|prehyphenchar|preexhyphenchar|predisplaysize|predisplaypenalty|predisplaygapfactor|predisplaydirection|prebinoppenalty|posthyphenchar|postexhyphenchar|postdisplaypenalty|permanent|penalty|pdfximage|pdfxformresources|pdfxformname|pdfxformmargin|pdfxformattr|pdfxform|pdfvorigin|pdfuniqueresname|pdfuniformdeviate|pdftrailerid|pdftrailer|pdftracingfonts|pdfthreadmargin|pdfthread|pdftexversion|pdftexrevision|pdftexbanner|pdfsuppressptexinfo|pdfsuppressoptionalinfo|pdfstartthread|pdfstartlink|pdfsetrandomseed|pdfsetmatrix|pdfsavepos|pdfsave|pdfretval|pdfrestore|pdfreplacefont|pdfrefximage|pdfrefxform|pdfrefobj|pdfrecompress|pdfrandomseed|pdfpxdimen|pdfprotrudechars|pdfprimitive|pdfpkresolution|pdfpkmode|pdfpkfixeddpi|pdfpagewidth|pdfpagesattr|pdfpageresources|pdfpageref|pdfpageheight|pdfpagebox|pdfpageattr|pdfoutput|pdfoutline|pdfomitcidset|pdfomitcharset|pdfobjcompresslevel|pdfobj|pdfnormaldeviate|pdfnoligatures|pdfnames|pdfminorversion|pdfmapline|pdfmapfile|pdfmajorversion|pdfliteral|pdflinkmargin|pdflastypos|pdflastxpos|pdflastximagepages|pdflastximage|pdflastxform|pdflastobj|pdflastlink|pdflastlinedepth|pdflastannot|pdfinsertht|pdfinfoomitdate|pdfinfo|pdfinclusionerrorlevel|pdfinclusioncopyfonts|pdfincludechars|pdfimageresolution|pdfimagehicolor|pdfimagegamma|pdfimageapplygamma|pdfimageaddfilename|pdfignoreunknownimages|pdfignoreddimen|pdfhorigin|pdfglyphtounicode|pdfgentounicode|pdfgamma|pdffontsize|pdffontobjnum|pdffontname|pdffontexpand|pdffontattr|pdffirstlineheight|pdfendthread|pdfendlink|pdfeachlineheight|pdfeachlinedepth|pdfdraftmode|pdfdestmargin|pdfdest|pdfdecimaldigits|pdfcreationdate|pdfcopyfont|pdfcompresslevel|pdfcolorstackinit|pdfcolorstack|pdfcatalog|pdfannot|pdfadjustspacing|pausing|patterns|parskip|parshapelength|parshapeindent|parshapedimen|parshape|parindent|parfillskip|pardirection|parattribute|parametermark|parametercount|par|pagevsize|pagetotal|pagestretch|pageshrink|pagegoal|pagefilstretch|pagefillstretch|pagefilllstretch|pagediscards|pagedepth|pageboundarypenalty|pageboundary|overwithdelims|overshoot|overloadmode|overloaded|overline|overfullrule|over|outputpenalty|outputbox|output|outer|orunless|orphanpenalty|orphanpenalties|orelse|or|omit|numexpression|numexpr|numericscale|number|nullfont|nulldelimiterspace|novrule|nospaces|normalyear|normalxtokspre|normalxtoksapp|normalxtoks|normalxspaceskip|normalxleaders|normalxdefcsname|normalxdef|normalwrapuppar|normalwordboundary|normalwidowpenalty|normalwidowpenalties|normalwd|normalvtop|normalvss|normalvsplit|normalvskip|normalvsize|normalvrule|normalvpack|normalvfuzz|normalvfilneg|normalvfill|normalvfil|normalvcenter|normalvbox|normalvbadness|normalvalign|normalvadjust|normaluppercase|normalunvpack|normalunvcopy|normalunvbox|normaluntraced|normalunskip|normalunpenalty|normalunletprotected|normalunletfrozen|normalunless|normalunkern|normalunhpack|normalunhcopy|normalunhbox|normalunexpandedloop|normalunexpanded|normalunderline|normalundent|normaluchyph|normaluccode|normaltracingstats|normaltracingrestores|normaltracingparagraphs|normaltracingpages|normaltracingoutput|normaltracingonline|normaltracingnodes|normaltracingnesting|normaltracingmath|normaltracingmarks|normaltracingmacros|normaltracinglostchars|normaltracinglevels|normaltracinginserts|normaltracingifs|normaltracinghyphenation|normaltracinggroups|normaltracingfullboxes|normaltracingfonts|normaltracingexpressions|normaltracingcommands|normaltracingassigns|normaltracingalignments|normaltracingadjusts|normaltpack|normaltoscaled|normaltopskip|normaltopmarks|normaltopmark|normaltomathstyle|normaltolerant|normaltolerance|normaltokspre|normaltoksdef|normaltoksapp|normaltoks|normaltokenized|normaltointeger|normaltodimension|normaltime|normalthinmuskip|normalthickmuskip|normalthewithoutunit|normalthe|normaltextstyle|normaltextfont|normaltextdirection|normaltabskip|normaltabsize|normalswapcsvalues|normalsupmarkmode|normalstring|normalsplittopskip|normalsplitmaxdepth|normalsplitfirstmarks|normalsplitfirstmark|normalsplitdiscards|normalsplitbotmarks|normalsplitbotmark|normalspan|normalspaceskip|normalspacefactor|normalsnapshotpar|normalskipdef|normalskip|normalskewchar|normalshowtokens|normalshowthe|normalshownodedetails|normalshowlists|normalshowifs|normalshowgroups|normalshowboxdepth|normalshowboxbreadth|normalshowbox|normalshow|normalshipout|normalshapingpenalty|normalshapingpenaltiesmode|normalsfcode|normalsetlanguage|normalsetfontid|normalsetbox|normalsemiprotected|normalsemiexpanded|normalscrollmode|normalscriptstyle|normalscriptspace|normalscriptscriptstyle|normalscriptscriptfont|normalscriptfont|normalscantokens|normalscantextokens|normalscaledfontdimen|normalsavingvdiscards|normalsavinghyphcodes|normalsavecatcodetable|normalrpcode|normalromannumeral|normalrightskip|normalrightmarginkern|normalrighthyphenmin|normalright|normalretokenized|normalrelpenalty|normalrelax|normalraise|normalradical|normalquitvmode|normalquitloop|normalpxdimen|normalprotrusionboundary|normalprotrudechars|normalprotected|normalprevgraf|normalprevdepth|normalpretolerance|normalprerelpenalty|normalprehyphenchar|normalpreexhyphenchar|normalpredisplaysize|normalpredisplaypenalty|normalpredisplaygapfactor|normalpredisplaydirection|normalprebinoppenalty|normalposthyphenchar|normalpostexhyphenchar|normalpostdisplaypenalty|normalpermanent|normalpenalty|normalpdfximage|normalpdfxformresources|normalpdfxformname|normalpdfxformmargin|normalpdfxformattr|normalpdfxform|normalpdfvorigin|normalpdfuniqueresname|normalpdfuniformdeviate|normalpdftrailerid|normalpdftrailer|normalpdftracingfonts|normalpdfthreadmargin|normalpdfthread|normalpdftexversion|normalpdftexrevision|normalpdftexbanner|normalpdfsuppressptexinfo|normalpdfsuppressoptionalinfo|normalpdfstartthread|normalpdfstartlink|normalpdfsetrandomseed|normalpdfsetmatrix|normalpdfsavepos|normalpdfsave|normalpdfretval|normalpdfrestore|normalpdfreplacefont|normalpdfrefximage|normalpdfrefxform|normalpdfrefobj|normalpdfrecompress|normalpdfrandomseed|normalpdfpxdimen|normalpdfprotrudechars|normalpdfprimitive|normalpdfpkresolution|normalpdfpkmode|normalpdfpkfixeddpi|normalpdfpagewidth|normalpdfpagesattr|normalpdfpageresources|normalpdfpageref|normalpdfpageheight|normalpdfpagebox|normalpdfpageattr|normalpdfoutput|normalpdfoutline|normalpdfomitcidset|normalpdfomitcharset|normalpdfobjcompresslevel|normalpdfobj|normalpdfnormaldeviate|normalpdfnoligatures|normalpdfnames|normalpdfminorversion|normalpdfmapline|normalpdfmapfile|normalpdfmajorversion|normalpdfliteral|normalpdflinkmargin|normalpdflastypos|normalpdflastxpos|normalpdflastximagepages|normalpdflastximage|normalpdflastxform|normalpdflastobj|normalpdflastlink|normalpdflastlinedepth|normalpdflastannot|normalpdfinsertht|normalpdfinfoomitdate|normalpdfinfo|normalpdfinclusionerrorlevel|normalpdfinclusioncopyfonts|normalpdfincludechars|normalpdfimageresolution|normalpdfimagehicolor|normalpdfimagegamma|normalpdfimageapplygamma|normalpdfimageaddfilename|normalpdfignoreunknownimages|normalpdfignoreddimen|normalpdfhorigin|normalpdfglyphtounicode|normalpdfgentounicode|normalpdfgamma|normalpdffontsize|normalpdffontobjnum|normalpdffontname|normalpdffontexpand|normalpdffontattr|normalpdffirstlineheight|normalpdfendthread|normalpdfendlink|normalpdfeachlineheight|normalpdfeachlinedepth|normalpdfdraftmode|normalpdfdestmargin|normalpdfdest|normalpdfdecimaldigits|normalpdfcreationdate|normalpdfcopyfont|normalpdfcompresslevel|normalpdfcolorstackinit|normalpdfcolorstack|normalpdfcatalog|normalpdfannot|normalpdfadjustspacing|normalpausing|normalpatterns|normalparskip|normalparshapelength|normalparshapeindent|normalparshapedimen|normalparshape|normalparindent|normalparfillskip|normalparfillleftskip|normalpardirection|normalparattribute|normalparametermark|normalparametercount|normalpar|normalpagevsize|normalpagetotal|normalpagestretch|normalpageshrink|normalpagegoal|normalpagefilstretch|normalpagefillstretch|normalpagefilllstretch|normalpagediscards|normalpagedepth|normalpageboundarypenalty|normalpageboundary|normaloverwithdelims|normalovershoot|normaloverloadmode|normaloverloaded|normaloverline|normaloverfullrule|normalover|normaloutputpenalty|normaloutputbox|normaloutput|normalouter|normalorunless|normalorphanpenalty|normalorphanpenalties|normalorelse|normalor|normalomit|normalnumexpression|normalnumexpr|normalnumericscale|normalnumber|normalnullfont|normalnulldelimiterspace|normalnovrule|normalnospaces|normalnormalizelinemode|normalnorelax|normalnonstopmode|normalnonscript|normalnolimits|normalnoindent|normalnohrule|normalnoexpand|normalnoboundary|normalnoaligned|normalnoalign|normalnewlinechar|normalmutoglue|normalmutable|normalmuskipdef|normalmuskip|normalmultiply|normalmugluespecdef|normalmuexpr|normalmskip|normalmoveright|normalmoveleft|normalmonth|normalmkern|normalmiddle|normalmessage|normalmedmuskip|normalmeaningless|normalmeaningfull|normalmeaningasis|normalmeaning|normalmaxdepth|normalmaxdeadcycles|normalmathsurroundskip|normalmathsurroundmode|normalmathsurround|normalmathstyle|normalmathscriptsmode|normalmathscriptcharmode|normalmathscriptboxmode|normalmathscale|normalmathrulethicknessmode|normalmathrulesmode|normalmathrulesfam|normalmathrel|normalmathrad|normalmathpunct|normalmathpenaltiesmode|normalmathord|normalmathopen|normalmathop|normalmathnolimitsmode|normalmathlimitsmode|normalmathinner|normalmathfrac|normalmathfontcontrol|normalmathflattenmode|normalmatheqnogapstep|normalmathdisplayskipmode|normalmathdirection|normalmathdelimitersmode|normalmathcontrolmode|normalmathcode|normalmathclose|normalmathchoice|normalmathchardef|normalmathchar|normalmathbin|normalmathaccent|normalmarks|normalmark|normalluatexversion|normalluatexrevision|normalluatexbanner|normalluafunctioncall|normalluafunction|normalluaescapestring|normalluadef|normalluacopyinputnodes|normalluabytecodecall|normalluabytecode|normallpcode|normallowercase|normallower|normallooseness|normallong|normallocalrightboxbox|normallocalrightbox|normallocalmiddleboxbox|normallocalmiddlebox|normallocalleftboxbox|normallocalleftbox|normallocalinterlinepenalty|normallocalcontrolledloop|normallocalcontrolled|normallocalcontrol|normallocalbrokenpenalty|normallineskiplimit|normallineskip|normallinepenalty|normallinedirection|normallimits|normallettonothing|normalletprotected|normalletfrozen|normalletcsname|normalletcharcode|normallet|normalleqno|normalleftskip|normalleftmarginkern|normallefthyphenmin|normalleft|normalleaders|normallccode|normallastskip|normallastpenalty|normallastparcontext|normallastnodetype|normallastnodesubtype|normallastnamedcs|normallastlinefit|normallastkern|normallastchknum|normallastchkdim|normallastbox|normallastarguments|normallanguage|normalkern|normaljobname|normalizelinemode|normalinterlinepenalty|normalinterlinepenalties|normalinteractionmode|normalintegerdef|normalinstance|normalinsertwidth|normalinsertuncopy|normalinsertunbox|normalinsertstoring|normalinsertstorage|normalinsertprogress|normalinsertpenalty|normalinsertpenalties|normalinsertmultiplier|normalinsertmode|normalinsertmaxdepth|normalinsertlimit|normalinsertheights|normalinsertheight|normalinsertdistance|normalinsertdepth|normalinsertcopy|normalinsertbox|normalinsert|normalinputlineno|normalinput|normalinitcatcodetable|normalinherited|normalindent|normalimmutable|normalimmediate|normalignorespaces|normalignorepars|normalignorearguments|normalifx|normalifvoid|normalifvmode|normalifvbox|normaliftrue|normaliftok|normalifrelax|normalifpdfprimitive|normalifpdfabsnum|normalifpdfabsdim|normalifparameters|normalifparameter|normalifodd|normalifnumval|normalifnumexpression|normalifnum|normalifmmode|normalifmathstyle|normalifmathparameter|normalifinsert|normalifinner|normalifincsname|normalifhmode|normalifhbox|normalifhasxtoks|normalifhastoks|normalifhastok|normalifhaschar|normaliffontchar|normalifflags|normaliffalse|normalifempty|normalifdimval|normalifdimexpression|normalifdim|normalifdefined|normalifcstok|normalifcsname|normalifcondition|normalifcmpnum|normalifcmpdim|normalifchknum|normalifchkdim|normalifcat|normalifcase|normalifboolean|normalifarguments|normalifabsnum|normalifabsdim|normalif|normalhyphenpenalty|normalhyphenchar|normalhyphenationmode|normalhyphenationmin|normalhyphenation|normalht|normalhss|normalhskip|normalhsize|normalhrule|normalhpack|normalholdinginserts|normalhjcode|normalhfuzz|normalhfilneg|normalhfill|normalhfil|normalhccode|normalhbox|normalhbadness|normalhangindent|normalhangafter|normalhalign|normalgtokspre|normalgtoksapp|normalglyphyscale|normalglyphyoffset|normalglyphxscale|normalglyphxoffset|normalglyphtextscale|normalglyphstatefield|normalglyphscriptscriptscale|normalglyphscriptscale|normalglyphscriptfield|normalglyphscale|normalglyphoptions|normalglyphdatafield|normalglyph|normalgluetomu|normalgluestretchorder|normalgluestretch|normalgluespecdef|normalglueshrinkorder|normalglueshrink|normalglueexpr|normalglobaldefs|normalglobal|normalglettonothing|normalgletcsname|normalglet|normalgleaders|normalgdefcsname|normalgdef|normalfuturelet|normalfutureexpandisap|normalfutureexpandis|normalfutureexpand|normalfuturedef|normalfuturecsname|normalfrozen|normalformatname|normalfonttextcontrol|normalfontspecyscale|normalfontspecxscale|normalfontspecscale|normalfontspecifiedsize|normalfontspecifiedname|normalfontspecid|normalfontspecdef|normalfontname|normalfontmathcontrol|normalfontid|normalfontdimen|normalfontcharwd|normalfontcharic|normalfontcharht|normalfontchardp|normalfont|normalflushmarks|normalfloatingpenalty|normalfirstvalidlanguage|normalfirstmarks|normalfirstmark|normalfinalhyphendemerits|normalfi|normalfam|normalexplicithyphenpenalty|normalexplicitdiscretionary|normalexpandtoken|normalexpandedloop|normalexpandedafter|normalexpanded|normalexpandcstoken|normalexpandafterspaces|normalexpandafterpars|normalexpandafter|normalexpand|normalexhyphenpenalty|normalexhyphenchar|normalexceptionpenalty|normaleveryvbox|normaleverytab|normaleverypar|normaleverymath|normaleveryjob|normaleveryhbox|normaleveryeof|normaleverydisplay|normaleverycr|normaleverybeforepar|normaletokspre|normaletoksapp|normaletoks|normalescapechar|normalerrorstopmode|normalerrorcontextlines|normalerrmessage|normalerrhelp|normaleqno|normalenforced|normalendsimplegroup|normalendmathgroup|normalendlocalcontrol|normalendlinechar|normalendinput|normalendgroup|normalendcsname|normalend|normalemergencystretch|normalelse|normalefcode|normaledefcsname|normaledef|normaldump|normaldp|normaldoublehyphendemerits|normaldivide|normaldisplaywidth|normaldisplaywidowpenalty|normaldisplaywidowpenalties|normaldisplaystyle|normaldisplaylimits|normaldisplayindent|normaldiscretionary|normaldirectlua|normaldimexpression|normaldimexpr|normaldimensiondef|normaldimendef|normaldimen|normaldetokenize|normaldelimitershortfall|normaldelimiterfactor|normaldelimiter|normaldelcode|normaldefcsname|normaldefaultskewchar|normaldefaulthyphenchar|normaldef|normaldeadcycles|normalday|normalcurrentmarks|normalcurrentloopnesting|normalcurrentloopiterator|normalcurrentiftype|normalcurrentiflevel|normalcurrentifbranch|normalcurrentgrouptype|normalcurrentgrouplevel|normalcsstring|normalcsname|normalcrcr|normalcrampedtextstyle|normalcrampedscriptstyle|normalcrampedscriptscriptstyle|normalcrampeddisplaystyle|normalcr|normalcountdef|normalcount|normalcopy|normalclubpenalty|normalclubpenalties|normalclearmarks|normalcleaders|normalchardef|normalchar|normalcatcodetable|normalcatcode|normalbrokenpenalty|normalboxyoffset|normalboxymove|normalboxxoffset|normalboxxmove|normalboxtotal|normalboxtarget|normalboxsource|normalboxshift|normalboxorientation|normalboxmaxdepth|normalboxgeometry|normalboxdirection|normalboxattribute|normalboxanchors|normalboxanchor|normalbox|normalboundary|normalbotmarks|normalbotmark|normalbinoppenalty|normalbelowdisplayskip|normalbelowdisplayshortskip|normalbeginsimplegroup|normalbeginmathgroup|normalbeginlocalcontrol|normalbegingroup|normalbegincsname|normalbatchmode|normalbaselineskip|normalbadness|normalautoparagraphmode|normalautomigrationmode|normalautomatichyphenpenalty|normalautomaticdiscretionary|normalattributedef|normalattribute|normalatopwithdelims|normalatop|normalatendofgrouped|normalatendofgroup|normalalltextstyles|normalallsplitstyles|normalallscriptstyles|normalallmathstyles|normalaligntab|normalalignmark|normalaligncontent|normalaliased|normalaftergrouped|normalaftergroup|normalafterassignment|normalafterassigned|normaladvance|normaladjustspacingstretch|normaladjustspacingstep|normaladjustspacingshrink|normaladjustspacing|normaladjdemerits|normalaccent|normalabovewithdelims|normalabovedisplayskip|normalabovedisplayshortskip|normalabove|normalXeTeXversion|normalUvextensible|normalUunderdelimiter|normalUsuperscript|normalUsuperprescript|normalUsubscript|normalUsubprescript|normalUstyle|normalUstopmath|normalUstopdisplaymath|normalUstartmath|normalUstartdisplaymath|normalUstack|normalUskewedwithdelims|normalUskewed|normalUroot|normalUright|normalUradical|normalUoverwithdelims|normalUoverdelimiter|normalUover|normalUnosuperscript|normalUnosuperprescript|normalUnosubscript|normalUnosubprescript|normalUmiddle|normalUmathyscale|normalUmathxscale|normalUmathvoid|normalUmathvextensiblevariant|normalUmathunderlinevariant|normalUmathunderdelimitervgap|normalUmathunderdelimitervariant|normalUmathunderdelimiterbgap|normalUmathunderbarvgap|normalUmathunderbarrule|normalUmathunderbarkern|normalUmathtopaccentvariant|normalUmathsupsubbottommax|normalUmathsupshiftup|normalUmathsupshiftdrop|normalUmathsuperscriptvariant|normalUmathsupbottommin|normalUmathsubtopmax|normalUmathsubsupvgap|normalUmathsubsupshiftdown|normalUmathsubshiftdrop|normalUmathsubshiftdown|normalUmathsubscriptvariant|normalUmathstackvgap|normalUmathstackvariant|normalUmathstacknumup|normalUmathstackdenomdown|normalUmathspacingmode|normalUmathspacebeforescript|normalUmathspaceafterscript|normalUmathskewedfractionvgap|normalUmathskewedfractionhgap|normalUmathrelrelspacing|normalUmathrelradspacing|normalUmathrelpunctspacing|normalUmathrelordspacing|normalUmathrelopspacing|normalUmathrelopenspacing|normalUmathrelinnerspacing|normalUmathrelfracspacing|normalUmathrelclosespacing|normalUmathrelbinspacing|normalUmathradrelspacing|normalUmathradradspacing|normalUmathradpunctspacing|normalUmathradordspacing|normalUmathradopspacing|normalUmathradopenspacing|normalUmathradinnerspacing|normalUmathradicalvgap|normalUmathradicalvariant|normalUmathradicalrule|normalUmathradicalkern|normalUmathradicaldegreeraise|normalUmathradicaldegreebefore|normalUmathradicaldegreeafter|normalUmathradfracspacing|normalUmathradclosespacing|normalUmathradbinspacing|normalUmathquad|normalUmathpunctrelspacing|normalUmathpunctradspacing|normalUmathpunctpunctspacing|normalUmathpunctordspacing|normalUmathpunctopspacing|normalUmathpunctopenspacing|normalUmathpunctinnerspacing|normalUmathpunctfracspacing|normalUmathpunctclosespacing|normalUmathpunctbinspacing|normalUmathphantom|normalUmathoverlinevariant|normalUmathoverlayaccentvariant|normalUmathoverdelimitervgap|normalUmathoverdelimitervariant|normalUmathoverdelimiterbgap|normalUmathoverbarvgap|normalUmathoverbarrule|normalUmathoverbarkern|normalUmathordrelspacing|normalUmathordradspacing|normalUmathordpunctspacing|normalUmathordordspacing|normalUmathordopspacing|normalUmathordopenspacing|normalUmathordinnerspacing|normalUmathordfracspacing|normalUmathordclosespacing|normalUmathordbinspacing|normalUmathoprelspacing|normalUmathopradspacing|normalUmathoppunctspacing|normalUmathopordspacing|normalUmathopopspacing|normalUmathopopenspacing|normalUmathopinnerspacing|normalUmathopfracspacing|normalUmathoperatorsize|normalUmathopenupheight|normalUmathopenupdepth|normalUmathopenrelspacing|normalUmathopenradspacing|normalUmathopenpunctspacing|normalUmathopenordspacing|normalUmathopenopspacing|normalUmathopenopenspacing|normalUmathopeninnerspacing|normalUmathopenfracspacing|normalUmathopenclosespacing|normalUmathopenbinspacing|normalUmathopclosespacing|normalUmathopbinspacing|normalUmathnumeratorvariant|normalUmathnolimitsupfactor|normalUmathnolimitsubfactor|normalUmathnolimits|normalUmathnoaxis|normalUmathlimits|normalUmathlimitbelowvgap|normalUmathlimitbelowkern|normalUmathlimitbelowbgap|normalUmathlimitabovevgap|normalUmathlimitabovekern|normalUmathlimitabovebgap|normalUmathinnerrelspacing|normalUmathinnerradspacing|normalUmathinnerpunctspacing|normalUmathinnerordspacing|normalUmathinneropspacing|normalUmathinneropenspacing|normalUmathinnerinnerspacing|normalUmathinnerfracspacing|normalUmathinnerclosespacing|normalUmathinnerbinspacing|normalUmathhextensiblevariant|normalUmathfractionvariant|normalUmathfractionrule|normalUmathfractionnumvgap|normalUmathfractionnumup|normalUmathfractiondenomvgap|normalUmathfractiondenomdown|normalUmathfractiondelsize|normalUmathfracrelspacing|normalUmathfracradspacing|normalUmathfracpunctspacing|normalUmathfracordspacing|normalUmathfracopspacing|normalUmathfracopenspacing|normalUmathfracinnerspacing|normalUmathfracfracspacing|normalUmathfracclosespacing|normalUmathfracbinspacing|normalUmathextrasupspace|normalUmathextrasupshift|normalUmathextrasupprespace|normalUmathextrasuppreshift|normalUmathextrasubspace|normalUmathextrasubshift|normalUmathextrasubprespace|normalUmathextrasubpreshift|normalUmathdenominatorvariant|normalUmathdelimiterundervariant|normalUmathdelimiterovervariant|normalUmathdegreevariant|normalUmathconnectoroverlapmin|normalUmathcodenum|normalUmathcode|normalUmathcloserelspacing|normalUmathcloseradspacing|normalUmathclosepunctspacing|normalUmathcloseordspacing|normalUmathcloseopspacing|normalUmathcloseopenspacing|normalUmathcloseinnerspacing|normalUmathclosefracspacing|normalUmathcloseclosespacing|normalUmathclosebinspacing|normalUmathclass|normalUmathcharslot|normalUmathcharnumdef|normalUmathcharnum|normalUmathcharfam|normalUmathchardef|normalUmathcharclass|normalUmathchar|normalUmathbotaccentvariant|normalUmathbinrelspacing|normalUmathbinradspacing|normalUmathbinpunctspacing|normalUmathbinordspacing|normalUmathbinopspacing|normalUmathbinopenspacing|normalUmathbininnerspacing|normalUmathbinfracspacing|normalUmathbinclosespacing|normalUmathbinbinspacing|normalUmathaxis|normalUmathadapttoright|normalUmathadapttoleft|normalUmathaccentvariant|normalUmathaccentbaseheight|normalUmathaccent|normalUleft|normalUhextensible|normalUdelimiterunder|normalUdelimiterover|normalUdelimiter|normalUdelcodenum|normalUdelcode|normalUchar|normalUatopwithdelims|normalUatop|normalUabovewithdelims|normalUabove|normalUUskewedwithdelims|normalUUskewed|normalOmegaversion|normalOmegarevision|normalOmegaminorversion|normalAlephversion|normalAlephrevision|normalAlephminorversion|normal |norelax|nonstopmode|nonscript|nolimits|noindent|nohrule|noexpand|noboundary|noaligned|noalign|newlinechar|mutoglue|mutable|muskipdef|muskip|multiply|mugluespecdef|muexpr|mskip|moveright|moveleft|month|mkern|middle|message|medmuskip|meaningless|meaningfull|meaningasis|meaning|maxdepth|maxdeadcycles|mathsurroundskip|mathsurroundmode|mathsurround|mathstyle|mathscriptsmode|mathscriptcharmode|mathscriptboxmode|mathscale|mathrulethicknessmode|mathrulesmode|mathrulesfam|mathrel|mathrad|mathpunct|mathpenaltiesmode|mathord|mathopen|mathop|mathnolimitsmode|mathlimitsmode|mathinner|mathfrac|mathfontcontrol|mathflattenmode|matheqnogapstep|mathdisplayskipmode|mathdirection|mathdelimitersmode|mathcontrolmode|mathcode|mathclose|mathchoice|mathchardef|mathchar|mathbin|mathaccent|marks|mark|luatexversion|luatexrevision|luatexbanner|luafunctioncall|luafunction|luaescapestring|luadef|luacopyinputnodes|luabytecodecall|luabytecode|lpcode|lowercase|lower|looseness|long|localrightboxbox|localrightbox|localmiddleboxbox|localmiddlebox|localleftboxbox|localleftbox|localinterlinepenalty|localcontrolledloop|localcontrolled|localcontrol|localbrokenpenalty|lineskiplimit|lineskip|linepenalty|linedirection|limits|lettonothing|letprotected|letfrozen|letcsname|letcharcode|let|leqno|leftskip|leftmarginkern|lefthyphenmin|left|leaders|lccode|lastskip|lastpenalty|lastparcontext|lastnodetype|lastnodesubtype|lastnamedcs|lastlinefit|lastkern|lastchknum|lastchkdim|lastbox|lastarguments|language|kern|jobname|interlinepenalty|interlinepenalties|interactionmode|integerdef|instance|insertwidth|insertuncopy|insertunbox|insertstoring|insertstorage|insertprogress|insertpenalty|insertpenalties|insertmultiplier|insertmode|insertmaxdepth|insertlimit|insertheights|insertheight|insertdistance|insertdepth|insertcopy|insertbox|insert|inputlineno|input|initcatcodetable|inherited|indent|immutable|immediate|ignorespaces|ignorepars|ignorearguments|ifx|ifvoid|ifvmode|ifvbox|iftrue|iftok|ifrelax|ifpdfprimitive|ifpdfabsnum|ifpdfabsdim|ifparameters|ifparameter|ifodd|ifnumval|ifnumexpression|ifnum|ifmmode|ifmathstyle|ifmathparameter|ifinsert|ifinner|ifincsname|ifhmode|ifhbox|ifhasxtoks|ifhastoks|ifhastok|ifhaschar|iffontchar|ifflags|iffalse|ifempty|ifdimval|ifdimexpression|ifdim|ifdefined|ifcstok|ifcsname|ifcondition|ifcmpnum|ifcmpdim|ifchknum|ifchkdim|ifcat|ifcase|ifboolean|ifarguments|ifabsnum|ifabsdim|if|hyphenpenalty|hyphenchar|hyphenationmode|hyphenationmin|hyphenation|ht|hss|hskip|hsize|hrule|hpack|holdinginserts|hjcode|hfuzz|hfilneg|hfill|hfil|hccode|hbox|hbadness|hangindent|hangafter|halign|gtokspre|gtoksapp|glyphyscale|glyphyoffset|glyphxscale|glyphxoffset|glyphtextscale|glyphstatefield|glyphscriptscriptscale|glyphscriptscale|glyphscriptfield|glyphscale|glyphoptions|glyphdatafield|glyph|gluetomu|gluestretchorder|gluestretch|gluespecdef|glueshrinkorder|glueshrink|glueexpr|globaldefs|global|glettonothing|gletcsname|glet|gleaders|gdefcsname|gdef|futurelet|futureexpandisap|futureexpandis|futureexpand|futuredef|futurecsname|frozen|formatname|fonttextcontrol|fontspecyscale|fontspecxscale|fontspecscale|fontspecifiedsize|fontspecifiedname|fontspecid|fontspecdef|fontname|fontmathcontrol|fontid|fontdimen|fontcharwd|fontcharic|fontcharht|fontchardp|font|flushmarks|floatingpenalty|firstvalidlanguage|firstmarks|firstmark|finalhyphendemerits|fi|fam|explicithyphenpenalty|explicitdiscretionary|expandtoken|expandedloop|expandedafter|expandcstoken|expandafterspaces|expandafterpars|expandafter|expand|exhyphenpenalty|exhyphenchar|exceptionpenalty|everyvbox|everytab|everypar|everymath|everyjob|everyhbox|everyeof|everydisplay|everycr|everybeforepar|etokspre|etoksapp|etoks|escapechar|errorstopmode|errorcontextlines|errmessage|errhelp|eqno|enforced|endsimplegroup|endmathgroup|endlocalcontrol|endlinechar|endinput|endgroup|endcsname|end|emergencystretch|else|efcode|edefcsname|edef|dump|dp|doublehyphendemerits|divide|displaywidth|displaywidowpenalty|displaywidowpenalties|displaystyle|displaylimits|displayindent|discretionary|directlua|dimexpression|dimexpr|dimensiondef|dimendef|dimen|detokenize|delimitershortfall|delimiterfactor|delimiter|delcode|defcsname|defaultskewchar|defaulthyphenchar|def|deadcycles|day|currentmarks|currentloopnesting|currentloopiterator|currentiftype|currentiflevel|currentifbranch|currentgrouptype|currentgrouplevel|csstring|csname|crcr|crampedtextstyle|crampedscriptstyle|crampedscriptscriptstyle|crampeddisplaystyle|cr|countdef|count|copy|clubpenalty|clubpenalties|clearmarks|cleaders|chardef|char|catcodetable|catcode|brokenpenalty|boxyoffset|boxymove|boxxoffset|boxxmove|boxtotal|boxtarget|boxsource|boxshift|boxorientation|boxmaxdepth|boxgeometry|boxdirection|boxattribute|boxanchors|boxanchor|box|boundary|botmarks|botmark|binoppenalty|belowdisplayskip|belowdisplayshortskip|beginsimplegroup|beginmathgroup|beginlocalcontrol|begingroup|begincsname|batchmode|baselineskip|badness|autoparagraphmode|automigrationmode|automatichyphenpenalty|automaticdiscretionary|attributedef|attribute|atopwithdelims|atop|atendofgrouped|atendofgroup|alltextstyles|allsplitstyles|allscriptstyles|allmathstyles|aligntab|alignmark|aligncontent|aliased|aftergrouped|aftergroup|afterassignment|afterassigned|advance|adjustspacingstretch|adjustspacingstep|adjustspacingshrink|adjustspacing|adjdemerits|accent|abovewithdelims|abovedisplayskip|abovedisplayshortskip|above|XeTeXversion|Uvextensible|Uunderdelimiter|Usuperscript|Usuperprescript|Usubscript|Usubprescript|Ustyle|Ustopmath|Ustopdisplaymath|Ustartmath|Ustartdisplaymath|Ustack|Uskewedwithdelims|Uskewed|Uroot|Uright|Uradical|Uoverwithdelims|Uoverdelimiter|Uover|Unosuperscript|Unosuperprescript|Unosubscript|Unosubprescript|Umiddle|Umathyscale|Umathxscale|Umathvoid|Umathvextensiblevariant|Umathunderlinevariant|Umathunderdelimitervgap|Umathunderdelimitervariant|Umathunderdelimiterbgap|Umathunderbarvgap|Umathunderbarrule|Umathunderbarkern|Umathtopaccentvariant|Umathsupsubbottommax|Umathsupshiftup|Umathsupshiftdrop|Umathsuperscriptvariant|Umathsupbottommin|Umathsubtopmax|Umathsubsupvgap|Umathsubsupshiftdown|Umathsubshiftdrop|Umathsubshiftdown|Umathsubscriptvariant|Umathstackvgap|Umathstackvariant|Umathstacknumup|Umathstackdenomdown|Umathspacingmode|Umathspacebeforescript|Umathspaceafterscript|Umathskewedfractionvgap|Umathskewedfractionhgap|Umathrelrelspacing|Umathrelradspacing|Umathrelpunctspacing|Umathrelordspacing|Umathrelopspacing|Umathrelopenspacing|Umathrelinnerspacing|Umathrelfracspacing|Umathrelclosespacing|Umathrelbinspacing|Umathradrelspacing|Umathradradspacing|Umathradpunctspacing|Umathradordspacing|Umathradopspacing|Umathradopenspacing|Umathradinnerspacing|Umathradicalvgap|Umathradicalvariant|Umathradicalrule|Umathradicalkern|Umathradicaldegreeraise|Umathradicaldegreebefore|Umathradicaldegreeafter|Umathradfracspacing|Umathradclosespacing|Umathradbinspacing|Umathquad|Umathpunctrelspacing|Umathpunctradspacing|Umathpunctpunctspacing|Umathpunctordspacing|Umathpunctopspacing|Umathpunctopenspacing|Umathpunctinnerspacing|Umathpunctfracspacing|Umathpunctclosespacing|Umathpunctbinspacing|Umathphantom|Umathoverlinevariant|Umathoverlayaccentvariant|Umathoverdelimitervgap|Umathoverdelimitervariant|Umathoverdelimiterbgap|Umathoverbarvgap|Umathoverbarrule|Umathoverbarkern|Umathordrelspacing|Umathordradspacing|Umathordpunctspacing|Umathordordspacing|Umathordopspacing|Umathordopenspacing|Umathordinnerspacing|Umathordfracspacing|Umathordclosespacing|Umathordbinspacing|Umathoprelspacing|Umathopradspacing|Umathoppunctspacing|Umathopordspacing|Umathopopspacing|Umathopopenspacing|Umathopinnerspacing|Umathopfracspacing|Umathoperatorsize|Umathopenupheight|Umathopenupdepth|Umathopenrelspacing|Umathopenradspacing|Umathopenpunctspacing|Umathopenordspacing|Umathopenopspacing|Umathopenopenspacing|Umathopeninnerspacing|Umathopenfracspacing|Umathopenclosespacing|Umathopenbinspacing|Umathopclosespacing|Umathopbinspacing|Umathnumeratorvariant|Umathnolimitsupfactor|Umathnolimitsubfactor|Umathnolimits|Umathnoaxis|Umathlimits|Umathlimitbelowvgap|Umathlimitbelowkern|Umathlimitbelowbgap|Umathlimitabovevgap|Umathlimitabovekern|Umathlimitabovebgap|Umathinnerrelspacing|Umathinnerradspacing|Umathinnerpunctspacing|Umathinnerordspacing|Umathinneropspacing|Umathinneropenspacing|Umathinnerinnerspacing|Umathinnerfracspacing|Umathinnerclosespacing|Umathinnerbinspacing|Umathhextensiblevariant|Umathfractionvariant|Umathfractionrule|Umathfractionnumvgap|Umathfractionnumup|Umathfractiondenomvgap|Umathfractiondenomdown|Umathfractiondelsize|Umathfracrelspacing|Umathfracradspacing|Umathfracpunctspacing|Umathfracordspacing|Umathfracopspacing|Umathfracopenspacing|Umathfracinnerspacing|Umathfracfracspacing|Umathfracclosespacing|Umathfracbinspacing|Umathextrasupspace|Umathextrasupshift|Umathextrasupprespace|Umathextrasuppreshift|Umathextrasubspace|Umathextrasubshift|Umathextrasubprespace|Umathextrasubpreshift|Umathdenominatorvariant|Umathdelimiterundervariant|Umathdelimiterovervariant|Umathdegreevariant|Umathconnectoroverlapmin|Umathcodenum|Umathcode|Umathcloserelspacing|Umathcloseradspacing|Umathclosepunctspacing|Umathcloseordspacing|Umathcloseopspacing|Umathcloseopenspacing|Umathcloseinnerspacing|Umathclosefracspacing|Umathcloseclosespacing|Umathclosebinspacing|Umathclass|Umathcharslot|Umathcharnumdef|Umathcharnum|Umathcharfam|Umathchardef|Umathcharclass|Umathchar|Umathbotaccentvariant|Umathbinrelspacing|Umathbinradspacing|Umathbinpunctspacing|Umathbinordspacing|Umathbinopspacing|Umathbinopenspacing|Umathbininnerspacing|Umathbinfracspacing|Umathbinclosespacing|Umathbinbinspacing|Umathaxis|Umathadapttoright|Umathadapttoleft|Umathaccentvariant|Umathaccentbaseheight|Umathaccent|Uleft|Uhextensible|Udelimiterunder|Udelimiterover|Udelimiter|Udelcodenum|Udelcode|Uchar|Uatopwithdelims|Uatop|Uabovewithdelims|Uabove|UUskewedwithdelims|UUskewed|Omegaversion|Omegarevision|Omegaminorversion|Alephversion|Alephrevision|Alephminorversion";
						foreach (string command in primitives.Split('|').Select(x => @"\" + x))
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
					DispatcherQueue.TryEnqueue(() =>
					{
						if (Codewriter.Language?.Name == name)
							VM.Codewriter.Language.Commands = lang.Commands;
						Codewriter.UpdateSuggestions();
					});
				});

			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
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

		private async void Btn_CommandGroup_Click(object sender, RoutedEventArgs e)
		{
			//InitializeCommandReference();
			try
			{
				IOrderedEnumerable<IGrouping<string, Command>> filtered = null;
				string text = Searchtext.Text;
				await Task.Run(() => { filtered = UpdateSearchFilter(text, VM.Default.FilterFavorites); });

				VM.ContextCommandGroupList = filtered.SelectMany(x => x).Select(x => x.Category).Distinct().ToList();
				cvs.Source = filtered;

				DocumentationView.SelectedIndex = -1;
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
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
					await Task.Run(() => { filtered = UpdateSearchFilter(text, VM.Default.FilterFavorites); });

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
		private async void FilterFavorites_Checked(object sender, RoutedEventArgs e)
		{
			try
			{

				bool ischecked = ((ToggleButton)sender).IsChecked ?? false;
				IOrderedEnumerable<IGrouping<string, Command>> filtered = null;
				string text = Searchtext.Text;
				await Task.Run(() =>
				{
					filtered = UpdateSearchFilter(text, ischecked);

					DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
							{
								VM.ContextCommandGroupList = filtered.SelectMany(x => x).Select(x => x.Category).Distinct().ToList();
								cvs.Source = filtered;

								DocumentationView.SelectedIndex = -1;
							});
				});
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
		}

		private async void AddDeleteFavorite_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				bool ischecked = ((ToggleButton)sender).IsChecked ?? false;
				Command cmd = ((ToggleButton)sender).DataContext as Command;

				if (ischecked)
				{
					if (!VM.Default.CommandFavorites.Any(x => x.Name == @"\" + (cmd.Type == "environment" ? "start" : "") + cmd.Name && x.ID == cmd.ID))
						VM.Default.CommandFavorites.Add(new(@"\" + (cmd.Type == "environment" ? "start" : "") + cmd.Name, cmd.ID));
				}
				else
				{
					VM.Default.CommandFavorites.Remove(VM.Default.CommandFavorites.First(x => x.Name == @"\" + (cmd.Type == "environment" ? "start" : "") + cmd.Name && x.ID == cmd.ID));

					if (VM.Default.FilterFavorites)
					{
						IOrderedEnumerable<IGrouping<string, Command>> filtered = null;
						string text = Searchtext.Text;
						await Task.Run(() => { filtered = UpdateSearchFilter(text, true); });

						VM.ContextCommandGroupList = filtered.SelectMany(x => x).Select(x => x.Category).Distinct().ToList();
						cvs.Source = filtered;
						DocumentationView.SelectedIndex = -1;
					}
				}
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
		}


		private IOrderedEnumerable<IGrouping<string, Command>> UpdateSearchFilter(string text, bool filterfavorites = false)
		{

			var selectedgroups = VM.Default.CommandGroups.Where(x => x.IsSelected).Select(x => x.Name);
			IOrderedEnumerable<IGrouping<string, Command>> query = null;
			if (!filterfavorites)
			{
				query = from item in contextcommands
												where item.Name.Insert(0, @"\" + (item.Type == "environment" ? "start" : "")).Contains(text, StringComparison.InvariantCultureIgnoreCase) && selectedgroups.Contains(item.Category)
												group item by item?.Category into g
												where !string.IsNullOrWhiteSpace(g.Key)
												orderby g.Key
												select g;
			}
			else
			{
				query = from item in contextcommands
												where item.IsFavorite
												where item.Name.Insert(0, @"\" + (item.Type == "environment" ? "start" : "")).Contains(text, StringComparison.InvariantCultureIgnoreCase) && selectedgroups.Contains(item.Category)
												group item by item?.Category into g
												where !string.IsNullOrWhiteSpace(g.Key)
												orderby g.Key
												select g;
			}

			//return from item in query
			//							where VM.Default.CommandGroups.Where(x => x.IsSelected).Select(x => x.Name).Contains(item.Key)
			//							orderby item.Key
			//							select item;
			return query;
		}

		private async void Btn_Clear_Click(object sender, RoutedEventArgs e)
		{
			VM.IsSaving = true;
			if (!VM.IsInstalling)
			{
				VM.IsIndeterminate = true;
				TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.Indeterminate);
			}

			await VM.ClearProject(VM.CurrentProject);

			VM.IsSaving = false;
			if (!VM.IsInstalling)
			{
				TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.NoProgress);
			}
		}

		private void Btn_CloseError_Click(object sender, RoutedEventArgs e)
		{
			VM.IsTeXError = false;
			if (!VM.IsInstalling)
			{
				VM.IsLoadingBarVisible = false;
				TaskbarUtility.SetProgressState(TaskbarProgressBarStatus.NoProgress);
			}
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

			CodeWriter cw = ((CodeWriter)sender);
			if (VM.Default.ShowOutline && (cw.Language.Name == "ConTeXt" | cw.Language.Name == "Markdown"))
				VM.UpdateOutline(cw.Lines.ToList(), true);

			if (cw.Language.Name == "Markdown" && VM.Default.InternalViewer && VM.Default.ShowMarkdownViewer)
			{
				if (cw.Text != VM.CurrentMarkdownText)
				{
					VM.MarkdownTimer.Stop();
					VM.MarkdownTimer.Start();
				}
			}
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

		private async void PDFjsViewer_SyncTeXRequested(object sender, double e)
		{
			try
			{
				var viewer = (PDFjsViewer)sender;
				int page = VM.Page;
				double scale = await viewer.CurrentScale;
				double yoffset = e;
				var synctex = VM.CurrentProject.SyncTeX;
				if (synctex != null)
				{
					var pageentries = synctex.SyncTeXEntries.Where(x => x.Page == page);
					var entry = pageentries.FirstOrDefault(x => x.YOffset > yoffset / scale / 2.03 - 2 && x.YOffset < yoffset / scale / 2.03 + 20);
					if (entry != null)
					{
						var file = synctex.SyncTeXInputFiles.FirstOrDefault(x => x.Id == entry.Id);
						if (file != null)
						{
							FileItem texfile = VM.CurrentProject.GetFileItemByName(VM.CurrentProject.Directory[0], Path.GetFileName(file.Name));
							VM.OpenFile(texfile);
							texfile.CurrentLine = new(0, entry.Line - 1);

						}
					}
				}
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
		}

		private void Codewriter_Initialized(object sender, EventArgs e)
		{
			((CodeWriter)sender).CenterView();
		}

		private void TabStripFooter_Loaded(object sender, RoutedEventArgs e)
		{
			if (App.MainWindow.IsCustomizationSupported)
			{
				TabStripFooter.SizeChanged += TabStripFooter_SizeChanged;
				TabStripFooter_SizeChanged(null, null);
			}
			else
			{
				//MainRibbon.TabStripFooter = null;
				//RootGrid.Background = null;
				//App.mainWindow.SetTitleBar(CustomDragRegion);
			}
		}

		private double scale = 0d;

		private void TabStripFooter_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			try
			{
				if (App.MainWindow.IsCustomizationSupported && App.MainWindow.AW != null)
				{
					if (scale == 0d)
					{
						scale = XamlRoot.RasterizationScale;
					}

					int width = (int)(XamlRoot.RasterizationScale * TabStripFooter.ActualWidth);
					int height = (int)(XamlRoot.RasterizationScale * TabStripFooter.ActualHeight);

					int x = (int)(XamlRoot.RasterizationScale * RootGrid.ActualWidth) - width;
					int y = 0;

					if (scale == XamlRoot.RasterizationScale)
					{

						App.MainWindow.AW.TitleBar.SetDragRectangles(new RectInt32[] { new RectInt32(x, y, width, height) });
					}
					else
					{
						scale = XamlRoot.RasterizationScale;
						App.MainWindow.AW.TitleBar.ResetToDefault();
						App.MainWindow.ResetTitleBar();
						var setting = ((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]);
						if (App.MainWindow.IsCustomizationSupported)
						{
							App.MainWindow.AW.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
							App.MainWindow.AW.TitleBar.ButtonBackgroundColor = Colors.Transparent;
							App.MainWindow.AW.TitleBar.ButtonHoverBackgroundColor = VM.Default.Backdrop == "Mica" ? Color.FromArgb(50, 125, 125, 125) : setting.AccentColor;
							App.MainWindow.AW.TitleBar.ButtonHoverForegroundColor = setting.ActualTheme == ApplicationTheme.Light ? Colors.Black : Colors.White;
							App.MainWindow.AW.TitleBar.ButtonForegroundColor = setting.ActualTheme == ApplicationTheme.Light ? Colors.Black : Colors.White;
							App.MainWindow.AW.TitleBar.ButtonInactiveForegroundColor = setting.ActualTheme == ApplicationTheme.Light ? Color.FromArgb(255, 50, 50, 50) : Color.FromArgb(255, 200, 200, 200);
						}
						else
						{
							Application.Current.Resources["WindowCaptionBackground"] = setting.AccentColorLow;
							Application.Current.Resources["WindowCaptionBackgroundDisabled"] = setting.AccentColorLow;
						}
						App.MainWindow.AW.TitleBar.SetDragRectangles(new RectInt32[] { new RectInt32(x, y, width, height) });
					}
				}
				else
				{
					//int width = (int)e.NewSize.Width;
					//int height = (int)e.NewSize.Height;
					//CustomDragRegion.Width = width;
					//CustomDragRegion.Height = height;
				}
			}
			catch (Exception ex)
			{
				VM.Log("Error at DPI change: " + ex.Message);
			}
		}

		private void SemanticZoom_ViewChangeStarted(object sender, SemanticZoomViewChangedEventArgs e)
		{
			try
			{
				if (e.IsSourceZoomedInView == false)
				{
					e.DestinationItem.Item = (cvs.Source as IOrderedEnumerable<IGrouping<string, Command>>).SelectMany(x => x).First(x => x.Category == (string)e.SourceItem.Item);
					VM.SelectedCommand = null;
					DocumentationView.ScrollIntoView(e.DestinationItem.Item);
				}
				else
				{
					GroupListView.SelectedItem = e.DestinationItem.Item = (e.SourceItem.Item as Command).Category;
				}
			}
			catch (Exception ex)
			{
				VM.Log(ex.Message);
			}
		}

		private delegate void ExceptionHandlerDelegate();
		private void ExceptionHandler(ExceptionHandlerDelegate exceptionHandlerDelegate)
		{
			try
			{
				exceptionHandlerDelegate();
			}
			catch (Exception ex)
			{
				VM?.Log(ex.Message);
			}
		}

		private async void DocumentationView_ItemClick(object sender, ItemClickEventArgs e)
		{
			//if (e.ClickedItem == DocumentationView.SelectedItem)
			//{
			ExceptionHandler(() =>
			{
				VM.SelectedCommand = (Command)e.ClickedItem;
			});
			//}
		}

		private string GetCommandString(Command command)
		{
			string text = @"\" + (command.Type == "environment" ? "start" : "") + command.Name;
			if (command?.Arguments?.ArgumentsList != null)
				foreach (var arg in command?.Arguments?.ArgumentsList)
					text += arg.Delimiters == "braces" ? "{}" : (arg.Delimiters == "none" ? "" : "[]");
			if (command.Type == "environment")
				text += "\n\t\n" + @"\stop" + command.Name;

			return text;
		}


		private void DocumentationView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			e.Data.RequestedOperation = DataPackageOperation.Copy;

			var command = (Command)e.Items.FirstOrDefault();
			if (command != null)
			{
				string texttopaste = GetCommandString(command);
				e.Data.SetText(texttopaste);
				e.Items[0] = texttopaste;
			}
		}

		private void DocumentationView_DragOver(object sender, DragEventArgs e)
		{
			e.DragUIOverride.Caption = "caption";
			e.DragUIOverride.IsGlyphVisible = false;
			e.DragUIOverride.IsCaptionVisible = false;
			e.Handled = true;
		}

		private void DocumentationView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (((FrameworkElement)e.OriginalSource).DataContext is Command cmd)
			{
				ListView listView = (ListView)sender;
				DocumentationViewContextMenu.ShowAt(listView, e.GetPosition(listView));
				tappedCommand = cmd;
			}
			else
				tappedCommand = null;
		}
		private Command tappedCommand = null;
		private void DocumentationViewContextMenu_Click(object sender, RoutedEventArgs e)
		{
			MenuFlyoutItem mfi = (MenuFlyoutItem)sender;
			Command command = tappedCommand;
			string texttocopy = "";
			switch (mfi.Tag)
			{
				case "copycommand": texttocopy = @"\" + (command.Type == "environment" ? "start" : "") + command.Name; break;
				case "copywitharguments": texttocopy = GetCommandString(command); break;
			}

			DataPackage dataPackage = new DataPackage();
			dataPackage.RequestedOperation = DataPackageOperation.Copy;
			dataPackage.SetText(texttocopy);
			Clipboard.SetContent(dataPackage);
		}

		private void Tbn_ShowOutline_Checked(object sender, RoutedEventArgs e)
		{
			VM.UpdateOutline(Codewriter.Lines.ToList(), true);
		}

		private void Codewriter_DoubleClicked(object sender, EventArgs e)
		{
			string text = Codewriter.SelectedText;
			if (text.StartsWith(@"\"))
			{

			}
		}

		private async void TbV_EditorTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (VM.Default.AutoOpenPDFOnFileOpen && e.AddedItems.Count > 0 && e.AddedItems[0] is FileItem fileitem && fileitem != null)
			{
				if (fileitem.IsTexFile)
				{
					StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Directory.GetParent(fileitem.File.Path).FullName);
					StorageFile pdfout = await folder.TryGetItemAsync(Path.GetFileNameWithoutExtension(fileitem.FileName) + ".pdf") as StorageFile;
					if (pdfout != null)
					{
						VM.Log("Opening " + Path.GetFileNameWithoutExtension(fileitem.FileName) + ".pdf");
						if (VM.Default.InternalViewer)
						{
							await OpenPDF(pdfout);

							if (VM.CurrentProject.UseSyncTeX)
							{
								string synctexpath = Path.GetFileNameWithoutExtension(fileitem.FileName) + ".synctex";
								StorageFile synctexfile = await folder.TryGetItemAsync(synctexpath) as StorageFile;
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
					}
				}
			}
		}

		private async void TgB_PDFWindow_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as ToggleButton).IsChecked == false)
			{
				VM.IsInternalViewerActive = true;
				await OpenPDF(null);
				pDFWindow?.Close();
				pDFWindow = null;
			}
			else
			{
				VM.IsInternalViewerActive = false;
				await OpenPDF(null);
			}
		}
	}
}
