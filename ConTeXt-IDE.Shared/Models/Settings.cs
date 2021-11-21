
using ConTeXt_IDE.Helpers;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.Storage;

namespace ConTeXt_IDE.Models
{
    public class Settings : Bindable
    {
        public static Settings FromJson(string json) => JsonConvert.DeserializeObject<Settings>(json);

        [JsonIgnore]
        public static Settings Default { get;  } = GetSettings();

        public static void RestoreSettings()
        {
            string file = "settings.json";
            var storageFolder = ApplicationData.Current.LocalFolder;
            string settingsPath = Path.Combine(storageFolder.Path, file);
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }
        }

        private static Settings GetSettings()
        {
            try
            {
                string file = "settings.json";
                var storageFolder = ApplicationData.Current.LocalFolder;
                string settingsPath = Path.Combine(storageFolder.Path, file);
                Settings settings;

                if (!File.Exists(settingsPath))
                {
                    settings = new Settings();
                    string json = settings.ToJson();
                    File.WriteAllText(settingsPath, json);
                }
                else
                {
                    string json = File.ReadAllText(settingsPath);
                    Debug.WriteLine(json);
                    settings = FromJson(json);
                }

                settings.PropertyChanged += (o, a) =>
                {
                    string json = settings.ToJson();
                    File.WriteAllText(settingsPath, json);
                };

                settings.ProjectList.CollectionChanged += (o, a) =>
                {
                    string json = settings.ToJson();
                    File.WriteAllText(settingsPath, json);
                };

                if (settings.HelpItemList.Count == 0)
                {
                    settings.HelpItemList =
                    new ObservableCollection<HelpItem>()
                        {
                            new HelpItem() { ID = "Modes", Title = "ConTeXt Modes", Text = "Select any number of modes. They will activate the corresponding \n'\\startmode[<ModeName>] ... \\stopmode'\n environments.", Shown = false },
                            new HelpItem() { ID = "Environments", Title = "ConTeXt Environments", Text = "Select any number of environments (usually one). Use this compiler parameter *instead* the corresponding \n'\\environment[<EnvironmentName>]'\n commands.", Shown = false },
                            new HelpItem() { ID = "AddProject", Title = "Add a Project", Text = "Click this button to open an existing project folder or to create a new project folder from a template.", Shown = false },
                        };
                }

                if (settings.ContextModules.Count == 0)
                {
                    settings.ContextModules = new ObservableCollection<ContextModule>() {
                        new ContextModule() { IsInstalled = false, Name = "filter", Description = "Process contents of a start-stop environment through an external program (Installed Pandoc needs to be in PATH!)", URL = @"https://modules.contextgarden.net/dl/t-filter-2020.06.29.zip", Type = ContextModuleType.TDSArchive},
                        new ContextModule() { IsInstalled = false, Name = "gnuplot", Description = "Inclusion of Gnuplot graphs in ConTeXt (Installed Gnuplot needs to be in PATH!)", URL = @"https://mirrors.ctan.org/macros/context/contrib/context-gnuplot.zip", Type = ContextModuleType.Archive, ArchiveFolderPath = @"context-gnuplot\"},
                        new ContextModule() { IsInstalled = false, Name = "letter", Description = "Package for writing letters", URL = @"https://mirrors.ctan.org/macros/context/contrib/context-letter.zip", Type = ContextModuleType.Archive, ArchiveFolderPath = @"context-letter\"},
                        new ContextModule() { IsInstalled = true, Name = "pgf", Description = "Create PostScript and PDF graphics in TeX", URL = @"http://mirrors.ctan.org/install/graphics/pgf/base/pgf.tds.zip", Type = ContextModuleType.TDSArchive},
                        new ContextModule() { IsInstalled = true, Name = "pgfplots", Description = "Create normal/logarithmic plots in two and three dimensions", URL = @"http://mirrors.ctan.org/install/graphics/pgf/contrib/pgfplots.tds.zip", Type = ContextModuleType.TDSArchive},
                    };
                }

                return settings;
            }
            catch (Exception ex)
            {
                //App.VM.Log("Exception on getting Settings: "+ex.Message);

                return null;
            }
        }

        public bool AutoOpenLOG { get => Get(false); set => Set(value); }
        public bool AutoOpenLOGOnlyOnError { get => Get(true); set => Set(value); }
        public bool AutoOpenPDF { get => Get(true); set => Set(value); }
        public bool DistributionInstalled { get => Get(false); set => Set(value); }
        public bool EvergreenInstalled { get => Get(false); set => Set(value); }
        public bool FirstStart { get => Get(true); set => Set(value); }
        public bool HelpPDFInInternalViewer { get => Get(false); set => Set(value); }
        public bool InternalViewer { get => Get(true); set => Set(value); }
        public bool MultiInstance { get => Get(false); set => Set(value); }
        public bool ShowLog { get => Get(false); set => Set(value); }
        public bool ShowOutline { get => Get(true); set => Set(value); }
        public bool ShowProjectPane { get => Get(true); set => Set(value); }
        public bool ShowProjects { get => Get(true); set => Set(value); }
        public bool ShowCommandReference { get => Get(false); set => Set(value); }
        public bool StartWithLastActiveProject { get => Get(true); set => Set(value); }
        public bool StartWithLastOpenFiles { get => Get(false); set => Set(value); }
        public bool SuggestArguments { get => Get(true); set => Set(value); }
        public bool SuggestCommands { get => Get(true); set => Set(value); }
        public bool SuggestFontSwitches { get => Get(true); set => Set(value); }
        public bool SuggestPrimitives { get => Get(true); set => Set(value); }
        public bool SuggestStartStop { get => Get(true); set => Set(value); }

        public bool TextWrapping { get => Get(false); set => Set(value); }
        public bool LineNumbers { get => Get(true); set => Set(value); }
        public bool LineMarkers { get => Get(true); set => Set(value); }
        public bool CodeFolding { get => Get(false); set => Set(value); }
        public bool ControlCharacters { get => Get(false); set => Set(value); }
       
        public string AccentColor { get => Get("Default"); set { Set(value); } }
       
        public string ContextDistributionPath { get => Get(ApplicationData.Current.LocalFolder.Path); set => Set(value); }
        public string ContextDownloadLink { get => Get(@"http://lmtx.pragma-ade.nl/install-lmtx/context-mswin.zip"); set => Set(value); }
        public string LastActiveProject { get => Get(""); set => Set(value); }
        public string Modes { get => Get(""); set => Set(value); }
        public string NavigationViewPaneMode { get => Get("Auto"); set => Set(value); }
        public string PackageID { get => Get(Package.Current.Id.FamilyName); set => Set(value); }
        public string TexFileFolder { get => Get(""); set => Set(value); }
        public string TexFileName { get => Get(""); set => Set(value); }
        public string TexFilePath { get => Get(""); set => Set(value); }
        public int FontSize { get => Get(18); set => Set(value); }
        public string Theme { get => Get("Default"); set {
                Set(value); 
                if (App.VM != null) 
                    ((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]).Theme = value == "Dark" ? ElementTheme.Dark : (value == "Light" ? ElementTheme.Light : ElementTheme.Default);
                if (App.MainPage != null)
                    App.MainPage.SetColor(null,false);
            } }


        
        public List<CommandGroup> CommandGroups { get => Get(new List<CommandGroup>()); set => Set(value); }

        public ObservableCollection<ContextModule> ContextModules { get => Get(new ObservableCollection<ContextModule>()); set => Set(value); }

        public ObservableCollection<Project> ProjectList { get => Get(new ObservableCollection<Project>()); set => Set(value); }
       
        public ObservableCollection<HelpItem> HelpItemList  {  get => Get(new ObservableCollection<HelpItem>()); set => Set(value); }

        
        [Newtonsoft.Json.JsonIgnore]
        public string[] ThemeOption => Enum.GetNames<ElementTheme>();
    }

    public static class Serialize
    {
        public static string ToJson(this Settings self) => JsonConvert.SerializeObject(self, Formatting.Indented);
    }

    public static class SettingsExtensions
    {
        public static void SaveSettings(this Settings settings)
        {
            string file = "settings.json";
            var storageFolder = ApplicationData.Current.LocalFolder;
            string settingsPath = Path.Combine(storageFolder.Path, file);
            string json = settings.ToJson();
            File.WriteAllText(settingsPath, json);
        }
    }
}