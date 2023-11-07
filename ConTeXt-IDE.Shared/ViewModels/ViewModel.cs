﻿using CodeEditorControl_WinUI;
using ConTeXt_IDE.Helpers;
using ConTeXt_IDE.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Search;
using Windows.UI;
using Windows.UI.StartScreen;

namespace ConTeXt_IDE.ViewModels
{
	public class ViewModel : Helpers.Bindable
	{
		public string LaunchArguments;
		public ObservableCollection<FileActivatedEventArgs> FileActivatedEvents = new ObservableCollection<FileActivatedEventArgs>() { };
		public ObservableCollection<FileItem> FileItemsTree = new ObservableCollection<FileItem>();
		public ObservableCollection<LogLine> LogLines { get => Get(new ObservableCollection<LogLine>()); set => Set(value); }
		public ObservableCollection<OutlineItem> OutlineItems { get => Get(new ObservableCollection<OutlineItem>()); set => Set(value); }

		public OutlineItem SelectedOutlineItem
		{
			get => Get<OutlineItem>(null);
			set
			{
				if (value?.Title != SelectedOutlineItem?.Title)
					Set(value);
			}
		}

		private string rootFilePath;

		public ObservableCollection<ContextModule> ContextModules { get => Get(new ObservableCollection<ContextModule>()); set => Set(value); }
		public ViewModel()
		{
			try
			{
				// Default = Settings.Default;
				FileItems = new ObservableCollection<FileItem>();
				CurrentFileItem = FileItems.Count > 0 ? FileItems.FirstOrDefault() : new FileItem(null);


				var modules = new ObservableCollection<ContextModule>() {
									new ContextModule() {  Name = "filter", Description = "Process contents of a start-stop environment through an external program (Installed Pandoc needs to be in PATH!)", URL = @"https://modules.contextgarden.net/dl/t-filter.zip", Type = ContextModuleType.TDSArchive},

									new ContextModule() {  Name = "vim", Description = "This module uses Vim editor's syntax files to syntax highlight verbatim code in ConTeXt (Module filter needs to be installed! Installed vim needs to be in PATH!)", URL = @"https://modules.contextgarden.net/dl/t-vim.zip", Type = ContextModuleType.TDSArchive},
									new ContextModule() {  Name = "annotation", Description = "Lets you create your own commands and environments to mark text blocks.", URL = @"https://modules.contextgarden.net/dl/t-annotation.zip", Type = ContextModuleType.TDSArchive},
									new ContextModule() {  Name = "simpleslides", Description = "A module for creating presentations in ConTeXt.", URL = @"https://modules.contextgarden.net/dl/t-simpleslides.zip", Type = ContextModuleType.TDSArchive},
									new ContextModule() {  Name = "gnuplot", Description = "Inclusion of Gnuplot graphs in ConTeXt (Installed Gnuplot needs to be in PATH!)", URL = @"https://mirrors.ctan.org/macros/context/contrib/context-gnuplot.zip", Type = ContextModuleType.Archive, ArchiveFolderPath = @"context-gnuplot\"},
									new ContextModule() {  Name = "letter", Description = "Package for writing letters", URL = @"https://modules.contextgarden.net/dl/t-letter.zip", Type = ContextModuleType.TDSArchive },
									new ContextModule() { Name = "pgf", Description = "Create PostScript and PDF graphics in TeX", URL = @"http://mirrors.ctan.org/install/graphics/pgf/base/pgf.tds.zip", Type = ContextModuleType.TDSArchive},
									new ContextModule() { Name = "pgfplots", Description = "Create normal/logarithmic plots in two and three dimensions", URL = @"http://mirrors.ctan.org/install/graphics/pgf/contrib/pgfplots.tds.zip", Type = ContextModuleType.TDSArchive},
					};
				foreach (var module in modules)
				{
					module.IsInstalled = Default.InstalledContextModules.Contains(module.Name);
				}
				ContextModules = modules;

				FileItems.CollectionChanged += FileItems_CollectionChanged1;

				MarkdownTimer.Elapsed += MarkdownTimer_Elapsed;

				if (Default.AccentColor == "Default")
				{
					if (AccentColors.Any(x => x.Color == SystemAccentColor))
						AccentColor = AccentColors.Find(x => x.Color == SystemAccentColor);
					else
						AccentColor = new AccentColor("Default", SystemAccentColor);
				}
				else
					AccentColor = AccentColors.Find(x => x.Name == Default.AccentColor);

				((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]).AccentColor = AccentColor.Color;
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

		private void MarkdownTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (IsMarkdownViewerActive && CurrentFileItem.FileContent != CurrentMarkdownText)
			{
				_queue.TryEnqueue(() =>
				{
					CurrentMarkdownText = CurrentFileItem.FileContent;
				});
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


		public string CurrentMarkdownText { get => Get(""); set => Set(value); }
		public AppServiceConnection AppServiceConnection { get; set; }

		public BackgroundTaskDeferral AppServiceDeferral { get; set; }

		public string Blocks { get => Get<string>(); set => Set(value); }
		public int Page { get => Get(1); set => Set(value); }
		public bool InfoOpen { get => Get(false); set => Set(value); }
		public bool CanUndo { get => Get(false); set => Set(value); }

		public string InfoTitle { get => Get(""); set => Set(value); }
		public string InfoText { get => Get(""); set => Set(value); }
		public InfoBarSeverity InfoSeverity { get => Get(InfoBarSeverity.Informational); set => Set(value); }

		public void InfoMessage(string infoTitle = "", string infoText = "", InfoBarSeverity infoSeverity = InfoBarSeverity.Informational)
		{
			InfoOpen = true;
			InfoTitle = infoTitle;
			InfoText = infoText;
			InfoSeverity = infoSeverity;
			Log(infoTitle + ": " + infoText);
		}

		public ConTeXtErrorMessage ConTeXtErrorMessage { get => Get(new ConTeXtErrorMessage()); set => Set(value); }


		public Timer MarkdownTimer = new(250) { AutoReset = true };
		public FileItem CurrentFileItem
		{
			get => Get(new FileItem(null));
			set
			{
				if (IsDragging | value == CurrentFileItem)
					return;

				Set(value);


				//if (string.Compare(value.File.Path,CurrentFileItem.File.Path)==0)
				//{
				//	return;
				//}


				OutlineItems.Clear();

				if (value == null)
				{
					IsMarkdownViewerActive = false;
					return;
				}
				if (value?.FileLanguage == "ConTeXt")
				{
					//UpdateOutline(value.FileContent);
					IsMarkdownViewerActive = false;
				}
				else if (value?.FileLanguage == "Markdown" && Default.ShowMarkdownViewer)
				{
					IsInternalViewerActive = false;
					IsMarkdownViewerActive = true;

					MarkdownViewerUriPrefix = value.FileFolder + "/";

					CurrentMarkdownText = value.FileContent;

					MarkdownTimer.Start();

				}
				else
				{
					IsMarkdownViewerActive = false;
				}

			}
		}

		public void UpdateOutline(List<Line> text = null, bool update = false)
		{
			if (text == null)
				text = Codewriter.Lines.ToList();

			UpdateOutline(text.Select(x => x.LineText).ToArray(), update);
		}

		public void UpdateOutline(string text = null, bool update = false)
		{
			if (text == null)
				text = String.Join("\r\n", Codewriter.Lines.Select(x => x.LineText));

			UpdateOutline(text.Split("\r\n"), update);
		}

		DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();

		public async void UpdateOutline(string[] text = null, bool update = false)
		{

			//await Task.Run(() =>
			//{

			_queue.TryEnqueue(() =>
			{

				if (CurrentFileItem != null)
					try
					{
						if (text == null)
							text = CurrentFileItem.FileContent.Split("\r\n", StringSplitOptions.None);

						if (!update)
							OutlineItems.Clear();

						string[] lines = text;

						List<OutlineItem> founditems = new();
						int row = 0;
						int ID = 0;
						foreach (string line in lines)
						{
							row++;
							MatchCollection mc = null;
							string depthcounter = "";
							int startdepth = 0;
							int depthgroup = 0;
							int titlegroup = 0;
							int level = 0;
							if (CurrentFileItem.FileLanguage == "ConTeXt")
							{
								mc = Regex.Matches(line, @"\\(start)?([sub].*?)?(section|subject|part|title|chapter)(\[.*?(title\s*?=)?\s*)(.+?)(\s*?)(\,|\])");
								depthgroup = 2;
								depthcounter = "sub";
								startdepth = 0;
								titlegroup = 6;
							}
							else if (CurrentFileItem.FileLanguage == "Markdown")
							{
								mc = Regex.Matches(line, @"(^ *?)(#+ *)(.*)");
								depthgroup = 2;
								depthcounter = "#";
								startdepth = -1;
								titlegroup = 3;

							}

							if (mc?.Count > 0 && mc.First().Success)
							{

								string type = "";
								string title = "";
								if (CurrentFileItem.FileLanguage == "ConTeXt")
								{
									level = startdepth + CountOccurenceswWithinString(mc.First().Groups.Values.ElementAt(depthgroup).Value, depthcounter);
									type = string.Concat(mc.First().Groups.Values.ToList().GetRange(1, 3).Select(x => x.Value)).Replace("start", "");
								}
								else if (CurrentFileItem.FileLanguage == "Markdown")
								{
									level = startdepth + CountOccurenceswWithinString(mc.First().Groups.Values.ElementAt(depthgroup).Value, depthcounter);
									if (level == 0)
										type = "section";
									else
										type = string.Concat(Enumerable.Repeat("sub", level)) + "section";
								}

								if (CurrentFileItem.FileLanguage == "ConTeXt")
								{
									title = mc.First().Groups.Values.ElementAt(titlegroup).Value.Replace("{", "").Replace("}", "");
								}
								else if (CurrentFileItem.FileLanguage == "Markdown")
								{
									title = mc.First().Groups.Values.ElementAt(titlegroup).Value;
								}

								if (!update)
									OutlineItems.Add(new OutlineItem() { Row = row, SectionLevel = level, SectionType = type, Title = title });
								else
								{
									founditems.Add(new OutlineItem() { Row = row, SectionLevel = level, SectionType = type, Title = title });
									if (OutlineItems.Any(x => x.Title == title && x.SectionLevel == level && x.SectionType == type))
									{
										OutlineItems.First(x => x.Title == title && x.SectionLevel == level && x.SectionType == type).Row = row;
									}
									else
									{
										if (OutlineItems.FirstOrDefault(x => x.Row > row) != null)
										{
											int index = OutlineItems.IndexOf(OutlineItems.FirstOrDefault(x => x.Row > row));
											if (index < OutlineItems.Count)
												OutlineItems.Insert(index, new OutlineItem() { Row = row, SectionLevel = level, SectionType = type, Title = title });
											else
												OutlineItems.Add(new OutlineItem() { Row = row, SectionLevel = level, SectionType = type, Title = title });
										}
										else
											OutlineItems.Add(new OutlineItem() { Row = row, SectionLevel = level, SectionType = type, Title = title });
									}
								}
							}
						}
						if (update)
						{
							foreach (var item in new List<OutlineItem>(OutlineItems))
							{
								if (!founditems.Any(x => x.Title == item.Title && x.SectionLevel == item.SectionLevel && x.SectionType == item.SectionType))
								{
									OutlineItems.Remove(item);
								}
							}
						}
						SelectedOutlineItem = OutlineItems.Where(x => CurrentFileItem?.CurrentLine?.iLine + 1 >= x.Row).LastOrDefault();
					}
					catch (Exception ex)
					{
						Log(ex.Message);
					}
			});
			//});
		}
		public int CountOccurenceswWithinString(string text, string searchterm)
		{
			int wordCount = 0;
			foreach (Match m in Regex.Matches(text, searchterm))
			{
				wordCount++;
			}

			return wordCount;
		}
		public FileItem CurrentRootItem
		{
			get => Get<FileItem>(null);
			set
			{
				if (CurrentRootItem != value)
				{
					Set(value);
					ResetRoot(CurrentProject.Directory);
					value.IsRoot = true;
				}
			}
		}

		private void ResetRoot(ObservableCollection<FileItem> fileItems)
		{
			foreach (var item in fileItems)
			{
				if (item.Type == FileItem.ExplorerItemType.ProjectRootFolder | item.Type == FileItem.ExplorerItemType.Folder)
				{
					ResetRoot(item.Children);
				}
				else if (item.File is StorageFile sfile)
				{
					item.IsRoot = false;
				}
			}
		}

		public Project CurrentProject
		{
			get => Get(new Project());
			set
			{
				if (value != null)
				{
					if (value.Folder == null && value.Name != null)
					{
						StorageFolder f = StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(value.Name).AsTask().Result;
						value.Folder = f;
					}
					IsProjectLoaded = value.Folder != null;
					if (value.Folder != null)
					{
						FileItemsTree?.Clear();
						FileItems?.Clear();
						watcher?.Dispose();
						GenerateTreeView(value.Folder, value.RootFilePath);
						value.Directory = FileItemsTree;
						Default.LastActiveProject = value.Name;
						IsProjectLoaded = true;
						IsTeXError = false;
						App.MainWindow.AW.Title = "ConTeXt IDE: " + value.Name;
						Log("Project " + value.Name + " loaded.");
					}
				}
				else
				{
					CurrentRootItem = null;

					IsProjectLoaded = false;
				}
				Set(value);
			}
		}

		public Settings Default { get; } = Settings.Default;

		public ObservableCollection<FileItem> FileItems { get => Get(new ObservableCollection<FileItem>()); set => Set(value); }

		public ObservableCollection<Helpfile> HelpFiles { get; } = new ObservableCollection<Helpfile>() {
												new Helpfile() { FriendlyName = "Manual", FileName = "ma-cb-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\manuals\" },
												new Helpfile() { FriendlyName = "Command Reference", FileName = "setup-en.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\qrcs\" },
													new Helpfile() { FriendlyName = "LuaMetaTeX (engine)", FileName = "luametatex.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\manuals\" },
													new Helpfile() { FriendlyName = "LuaMetaFun (MetaPost library)", FileName = "luametafun.pdf", Path = @"\tex\texmf-context\doc\context\documents\general\manuals\" },
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
		public Color SystemAccentColor { get => Get((new Windows.UI.ViewManagement.UISettings()).GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent)); set => Set(value); }

		public bool IsError { get => Get(false); set => Set(value); }
		public Thickness Margin_SettingsButton { get => Get(new Thickness(0)); set => Set(value); }
		public Thickness RibbonCornerRadius { get => Get(new Thickness(0)); set => Set(value); }
		public Thickness RibbonMargin { get => Get(new Thickness(0)); set => Set(value); }
		public Thickness InfobarMargin { get => Get(new Thickness(Default.RibbonMarginValue, 0, 0, 0)); set => Set(value); }
		public Thickness Margin_Ribbon { get => Get(new Thickness(Default.RibbonMarginValue, 0, Default.RibbonMarginValue, Default.RibbonMarginValue)); set { Set(value); InfobarMargin = value; } }
		public CornerRadius CornerRadius_Ribbon { get => Get(new CornerRadius(Default.RibbonMarginValue * 2)); set => Set(value); }
		public bool IsTeXError { get => Get(false); set => Set(value); }

		public bool IsIndeterminate { get => Get(true); set { Set(value); } }

		public double ProgressValue { get => Get(0d); set => Set(value); }

		public bool IsUsingTouch { get => Get(false); set { Set(value); SplitterWidth = value ? 5 : 3; PlacementMode = value ? NumberBoxSpinButtonPlacementMode.Inline : NumberBoxSpinButtonPlacementMode.Compact; } }
		public NumberBoxSpinButtonPlacementMode PlacementMode { get => Get(NumberBoxSpinButtonPlacementMode.Compact); set { Set(value); } }
		public int SplitterWidth { get => Get(3); set { Set(value); } }
		public bool IsFileItemLoaded { get => Get(false); set { Set(value); } }

		public bool IsInternalViewerActive { get => Get(false); set => Set(value); }

		public bool IsMarkdownViewerActive { get => Get(false); set => Set(value); }

		public string MarkdownViewerUriPrefix { get => Get("ms-appx://"); set => Set(value); }

		public bool IsPaused { get => Get(false); set { Set(value); } }

		public bool Started { get => Get(false); set { Set(value); } }

		public bool IsInstalled { get => Get(false); set { Set(value); } }

		public bool IsProjectLoaded { get => Get(false); set => Set(value); }

		public bool IsSaving
		{
			get => Get(false); set
			{
				Set(value);
				if (value) { IsLoadingBarVisible = true; IsPaused = false; }
				if (!value && !IsInstalling) { IsLoadingBarVisible = false; }
			}
		}

		public bool IsInstalling { get => Get(false); set { Set(value); if (value) { IsLoadingBarVisible = true; IsPaused = false; } if (!value && !IsSaving) { IsLoadingBarVisible = false; } } }

		public bool IsLoadingBarVisible { get => Get(false); set => Set(value); }
		public bool IsDragging { get => Get(false); set => Set(value); }
		public bool CloseRequested { get => Get(false); set => Set(value); }

		public string NVHead { get => Get(""); set => Set(value); }

		public Command SelectedCommand
		{
			get => Get<Command>(null);
			set
			{
				if (value == null)
				{
					if (SelectedCommand != null)
					{
						SelectedCommand.IsSelected = false; SelectedCommand.SelectedIndex = -1;
					}
					Set(value);
				}
				else if (value.Name == SelectedCommand?.Name && value.ID == SelectedCommand?.ID)
				{
					value.IsSelected = !value.IsSelected;
					value.SelectedIndex = -1;
				}
				else if (!value.IsSelected)
				{
					if (SelectedCommand != null)
					{
						SelectedCommand.IsSelected = false; SelectedCommand.SelectedIndex = -1;
					}

					Set(value);
					Task.Run(async () =>
				{
					_queue.TryEnqueue(async () =>
					{
						value.IsSelected = true;
					});
				});
				}
			}
		}

		public bool ShowCommandReference
		{
			get => Get(false); set
			{
				try
				{
					Set(value);
				}
				catch (Exception ex)
				{
					Log(ex.Message);
				}
			}
		}

		public List<string> ContextCommandGroupList { get => Get(new List<string>()); set => Set(value); }

		public CollectionViewSource cvs { get => Get(new CollectionViewSource() { IsSourceGrouped = true }); set => Set(value); }

		public ObservableCollection<ContextCommand> ContextCommands { get => Get(new ObservableCollection<ContextCommand>()); set => Set(value); }

		public List<string> InterfaceList { get => Get(new List<string>() { "en", "nl", "de", "it", "fr", "cs", "ro", "gb" }); set => Set(value); }


		public StorageItemMostRecentlyUsedList RecentAccessList { get => Get<StorageItemMostRecentlyUsedList>(); set => Set(value); }

		public string SelectedPath { get => Get(""); set => Set(value); }


		FileSystemWatcher watcher;
		public async Task GenerateTreeView(StorageFolder folder, string rootfile = null)
		{
			rootFilePath = rootfile;

			if (folder != null)
			{
				FileItemsTree.Add(new FileItem(folder) { IsExpanded = true, Type = FileItem.ExplorerItemType.ProjectRootFolder });
				await DirWalk(folder);
				if (CurrentRootItem != null)
					OpenFile(CurrentRootItem, true);

				watcher?.Dispose();

				watcher = new FileSystemWatcher(folder.Path) { IncludeSubdirectories = true, EnableRaisingEvents = true };

				watcher.NotifyFilter = NotifyFilters.DirectoryName
																															| NotifyFilters.FileName
																															| NotifyFilters.LastWrite;
				watcher.Created += (a, b) =>
				{
					string filename = Path.GetFileName(b.FullPath);
					bool iscompiledpdf = (filename.StartsWith("project_") | filename.StartsWith("prd_") | filename.StartsWith("c_") | filename.StartsWith("env_") | filename.StartsWith("p-") | filename.StartsWith("t-")) && filename.EndsWith(".pdf");
					if (!b.FullPath.EndsWith("~tmp") && !cancelWords.Contains(Path.GetExtension(b.FullPath)) && !iscompiledpdf)
					{
						_queue.TryEnqueue(async () =>
						{
							var item = await GetStorageItem(b.FullPath);
							if (item != null)
							{
								var fi = new FileItem(item);

								var info = Directory.GetParent(item.Path);

								var containingfolder = CurrentProject.GetDirectoryByPath(CurrentProject.Directory[0], info.FullName);
								if (containingfolder != null)
								{
									AddFileItemAplphabetically(containingfolder.Children, fi);
									Log($"{fi.Type.ToString()} {b.Name} created.");
								}
							}
						});
					}
				};
				watcher.Renamed += (a, b) =>
				{
					if (!b.FullPath.ToLower().EndsWith("tmp") && !b.OldFullPath.ToLower().EndsWith("tmp"))
					{
						_queue.TryEnqueue(async () =>
						{
							var item = await GetStorageItem(b.FullPath);
							if (item != null)
							{
								var fi = CurrentProject.GetFileItemByPath(CurrentProject?.Directory[0], b.OldFullPath);
								if (fi != null)
								{
									fi.File = item;
									fi.FileName = item.Name;
									if (fi.File is StorageFile sf)
									{
										fi.FileLanguage = FileItem.GetFileType(sf.FileType);
									}
									Log($"{fi.Type.ToString()} {b.OldName} was renamed to {b.Name}.");
								}
							}
						});
					}
				};
				watcher.Deleted += (a, b) =>
				{
					if (!b.FullPath.EndsWith("~tmp"))
					{
						_queue.TryEnqueue(async () =>
					{

						var fi = CurrentProject.RemoveFileItemByPath(CurrentProject?.Directory[0], b.FullPath);
						if (fi != null)
							Log($"{(fi.Type == FileItem.ExplorerItemType.File ? "File" : "Folder")} {b.Name} deleted.");
						//var item = await GetStorageItem(b.FullPath);
						//if (item != null)
						//{

						//}
					});
					}
				};
				watcher.Changed += (a, b) =>
				{
					if (!b.FullPath.EndsWith("~tmp") && !b.FullPath.EndsWith(".TMP") && b.ChangeType == WatcherChangeTypes.Changed)
					{
						_queue.TryEnqueue(async () =>
						{
							try
							{
								if (Directory.Exists(b.FullPath))
									return;
								var item = await GetStorageItem(b.FullPath);
								if (item != null)
								{
									var fileitem = CurrentProject.GetFileItemByPath(CurrentProject?.Directory[0], b.FullPath);

									if (fileitem != null && fileitem.Type == FileItem.ExplorerItemType.File)
									{
										string changedtext = await File.ReadAllTextAsync(b.FullPath);
										if (fileitem.FileContent != changedtext)
										{
											fileitem.LastSaveFileContent = changedtext;
											fileitem.FileContent = changedtext;
											Log($"File {b.Name} was changed outside of the app.");
										}
									}
								}
							}
							catch
							{

							}
						});
					}
				};

				// return true;
			}
			else
			{
				Log("Operation cancelled.");
				// return false;
			}
			return;
		}

		async Task<IStorageItem> GetStorageItem(string path)
		{
			try
			{
				if (Directory.Exists(path))
					return await StorageFolder.GetFolderFromPathAsync(path);

				else if (File.Exists(path))
					return await StorageFile.GetFileFromPathAsync(path);

				else
					return null;
			}
			catch
			{
				return null;
			}
		}

		public void AddFileItemAplphabetically(ObservableCollection<FileItem> Children, FileItem fi)
		{
			int count = Children.Count;
			if (count == 0)
			{
				Children.Add(fi);
				return;
			}

			var lastItem = Children.LastOrDefault(x => x.Type == fi.Type);
			if (lastItem != null)
			{
				if (fi.CompareTo(lastItem) > 0)
				{
					if (Children.IndexOf(lastItem) < Children.Count - 1)
						Children.Insert(Children.IndexOf(lastItem) + 1, fi);
					else
						Children.Add(fi);
					return;
				}
			}
			else
			{
				if (fi.Type == FileItem.ExplorerItemType.Folder)
				{
					Children.Insert(0, fi);
				}
				else
				{
					Children.Add(fi);
				}
				return;
			}

			if (fi.CompareTo(Children.Last()) > 0)
			{
				Children.Add(fi);
			}
			else
			{
				int insertindex = 0;
				for (int i = 0; i < count; i++)
				{
					if (Children[i].CompareTo(fi) > 0 && Children[i].Type == fi.Type)
					{
						insertindex = i;
						break;
					}
				}
				Children.Insert(insertindex, fi);
			}
		}

		private readonly List<string> cancelWords = new List<string> { ".gitignore", ".tuc", ".log", ".pgf", ".tua", ".synctex", ".syncctx", ".TMP" };
		private readonly List<string> auxillaryWords = new List<string> { ".tuc", ".log", ".pgf", ".synctex", ".syncctx", ".TMP" };
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

						fi.IsRoot = fi.File.Path == rootFilePath;
						if (fi.IsRoot)
							CurrentRootItem = fi;

						if (level > 0)
						{
							currFolder.Children.Add(fi);
							if (fi.IsRoot)
							{
								currFolder.IsExpanded = true;
							}
						}
						else
							FileItemsTree[0].Children.Add(fi);
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

		public int CurrentLogLine = 0;
		public void Log(object log)
		{
			try
			{
				//Blocks = log.ToString();
				CurrentLogLine++;
				LogLines.Add(new() { Line = CurrentLogLine.ToString(), Date = DateTime.Now.ToString("HH:mm:ss"), Message = log?.ToString() });
				// App.MainPage.Log.ItemsSource = LogLines;

			}
			catch
			{

			}
		}

		public Dictionary<string, string> Meta(string m)
		{
			return m.Split(';').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
		}

		public CodeWriter Codewriter { get => Get<CodeWriter>(); set => Set(value); }

		public void OpenFile(string fileName)
		{
			var fi = CurrentProject.Directory.FirstOrDefault(x => x.FileName == fileName);
			if (fi != null)
				OpenFile(fi);
		}

		public MainPage MainPage;
		public async void OpenFile(FileItem file, bool ProjectLoad = false)
		{
			try
			{
				if (!FileItems.Contains(file))
				{
					var openfile = FileItems.Where(x => x.File.Path == file.File.Path);

					var read = await FileIO.ReadTextAsync((StorageFile)file.File);
					file.LastSaveFileContent = read;
					//await Task.Delay(500);

					file.FileContent = read;

					if (openfile.Count() > 0)
					{
						if (CurrentFileItem == openfile.FirstOrDefault())
						{
							FileItems[FileItems.IndexOf(openfile.FirstOrDefault())] = file;
							//CurrentFileItem = File;
						}
						else
						{
							FileItems[FileItems.IndexOf(openfile.FirstOrDefault())] = file;
							//CurrentFileItem = File;
						}
					}
					else
					{
						FileItems.Add(file);
						//await Task.Delay(500); // ObservableCollection.Add() is slow, so setting CurrentFileItem = File will result in an exception because FileItems is still empty. A cleaner approach is needed. 
						file.IsOpened = true;
						Log("File " + file.FileName + " opened");
					}

					if (FileItems.Contains(file))
						CurrentFileItem = file;

				}
				else
				{
					CurrentFileItem = file;
				}
			}
			catch (Exception ex)
			{
				Log("Cannot open selected file: " + ex.Message);
			}
		}

		public async Task<bool> ClearWorkspace(StorageFolder f)
		{
			bool Success = true;
			try
			{
				foreach (StorageFolder folder in await f.GetFoldersAsync())
				{
					bool s = await ClearWorkspace(folder);
					if (!s)
						Success = false;
				}
				foreach (StorageFile item in await f.GetFilesAsync())
				{
					if (auxillaryWords.Contains(item.FileType))
					{
						if (FileItems.Any(x => x.FileName == item.Name))
							FileItems.Remove(FileItems.First(x => x.FileName == item.Name));
						await item.DeleteAsync();
					}
					else if (item.FileType == ".pdf")
					{
						string filename = item.Name;
						bool iscompiledpdf = (filename.StartsWith("project_") | filename.StartsWith("prd_") | filename.StartsWith("c_") | filename.StartsWith("env_") | filename.StartsWith("p-") | filename.StartsWith("t-"));
						if (iscompiledpdf)
							await item.DeleteAsync();
					}
				}
				return Success;
			}
			catch
			{
				return false;
			}
		}

		public async Task ClearProject(Project proj)
		{
			try
			{
				StorageFolder f = proj.Folder;

				await ClearWorkspace(f);

				Log($"Project {proj.Name} cleared.");
			}
			catch (Exception ex)
			{
				InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
			}
		}



		public async Task LoadProject(Project proj)
		{
			try
			{
				FileItems?.Clear();
				StorageFolder f = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(proj.Name);

				var options = new QueryOptions() { FolderDepth = FolderDepth.Deep };
				// options.ApplicationSearchFilter = "*.tex";
				var queryresult = f.CreateFileQueryWithOptions(options);

				queryresult.ContentsChanged += Queryresult_ContentsChanged; // this never fires. Why?

				await queryresult.GetFilesAsync();

				var list = Default.ProjectList.Where(x => x.Name == f.Name);

				if (list.Count() == 1)
				{
					var project = list.FirstOrDefault();

					project.Folder = f;
					FileItemsTree = new ObservableCollection<FileItem>();

					CurrentProject = project;
				}

				Default.LastActiveProject = proj.Name;

				if (!string.IsNullOrEmpty(CurrentProject?.RootFilePath))
				{
					//await Task.Delay(1000);
					if (!Default.StartWithLastOpenFiles && CurrentRootItem != null)
						OpenFile(CurrentRootItem, true);
					else if (Default.StartWithLastOpenFiles)
					{
						foreach (var item in CurrentProject.LastOpenedFiles)
						{
							FileItem fileItem = CurrentProject.Directory[0]?.Children?.FirstOrDefault(x => x.FileName == item);
							if (fileItem != null) OpenFile(fileItem);
						}
					}
					// await Task.Delay(500);
				}
			}
			catch (Exception ex)
			{
				InfoMessage("Error", ex.Message, InfoBarSeverity.Error);
			}
		}

		private void Queryresult_ContentsChanged(Windows.Storage.Search.IStorageQueryResultBase sender, object args)
		{
			//sender.ContentsChanged -= Queryresult_ContentsChanged;
			//LoadProject(CurrentProject);
		}

		public async void PopulateJumpList()
		{
			try
			{
				JumpList jl = await JumpList.LoadCurrentAsync();
				jl.Items.Clear();
				await jl.SaveAsync();

				jl.SystemGroupKind = JumpListSystemGroupKind.None;
				foreach (var item in Default.ProjectList)
				{
					JumpListItem jli = JumpListItem.CreateWithArguments(item.Name, item.Name);
					jli.GroupName = "ConTeXt Projects";
					jli.Description = item.Path;
					jli.Logo = new("ms-appx:///Assets/SquareLogo.png");
					jl.Items.Add(jli);
				}

				await jl.SaveAsync();
			}
			catch (Exception ex)
			{
				Log(ex.Message);
			}
		}

		public async void Startup()
		{
			try
			{
				if (Default.StartWithLastActiveProject && !string.IsNullOrWhiteSpace(Default.LastActiveProject) && string.IsNullOrWhiteSpace(LaunchArguments))
				{
					RecentAccessList = StorageApplicationPermissions.MostRecentlyUsedList;
					if (RecentAccessList.ContainsItem(Default.LastActiveProject))
					{

						IsSaving = true;
						var list = Default.ProjectList.Where(x => x.Name == Default.LastActiveProject);
						if (list != null && list.Count() == 1)
						{
							var project = list.FirstOrDefault();
							CurrentProject = project;

							//var folder = await RecentAccessList.GetFolderAsync(Default.LastActiveProject);
							//if (folder.Name != Default.LastActiveProject)
							//{
							//	RecentAccessList.Remove(Default.LastActiveProject);
							//	Default.LastActiveProject= CurrentProject.Name = RecentAccessList.Add(folder);
							//	PopulateJumpList();
							//}
						}
					}
				}
				else if (!string.IsNullOrWhiteSpace(LaunchArguments))
				{
					IsSaving = true;
					var list = Default.ProjectList.Where(x => x.Name == LaunchArguments);
					if (list != null && list.Count() == 1)
					{
						var project = Default.ProjectList.FirstOrDefault(x => x.Name == LaunchArguments);
						CurrentProject = project;
					}
				}

				PopulateJumpList();

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
							if (!IsInstalling)
								IsIndeterminate = true;
							IsError = false;
							IsSaving = true;
							string cont = filetosave.FileContent ?? " ";
							var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(cont, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
							await FileIO.WriteBufferAsync(CurrentFileItem.File as StorageFile, buffer);
							filetosave.LastSaveFileContent = filetosave.FileContent;
							filetosave.IsChanged = false;
							IsSaving = false;
							Log("File " + filetosave.File.Name + " saved");
							UpdateOutline(filetosave.FileContent);
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
								UpdateOutline(item.FileContent);
							}
						}
						IsSaving = false;
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
			// Dirty hack to overcome the issue, that the TabView reordering mechanism calls a Remove and an Add event instead of one Move event

			if (e.Action == NotifyCollectionChangedAction.Remove && (e.OldItems?[0] as FileItem) == CurrentFileItem)
			{
				if (!CloseRequested)
				{
					IsDragging = true;
				}
				CloseRequested = false;
			}
			else if (e.Action == NotifyCollectionChangedAction.Add && (e.NewItems?[0] as FileItem) == CurrentFileItem)
			{
				IsDragging = false;
			}

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

	public class LogLine
	{
		public string Line { get; set; }
		public string Date { get; set; }
		public string Message { get; set; }
	}
}