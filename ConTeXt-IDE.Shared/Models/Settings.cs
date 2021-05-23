
using ConTeXt_IDE.Helpers;
using Microsoft.UI.Xaml;
using Monaco.Editor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static Settings Default { get; } = GetSettings();

        public static Settings RestoreSettings()
        {
            string file = "settings.json";
            var storageFolder = ApplicationData.Current.LocalFolder;
            string settingsPath = Path.Combine(storageFolder.Path, file);
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }

            return GetSettings();
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
                new HelpItem() { ID = "AddProject", Title = "Add a Project", Text = "Click this button to open an existing project folder or to create a new project folder from a template.", Shown = false },
            };
                }

                return settings;
            }
            catch (Exception ex)
            {
                App.VM.Log(ex.Message);

                return null;
            }
        }

        public bool StartWithLastActiveProject { get => Get(true); set => Set(value); }
        public bool MultiInstance { get => Get(false); set => Set(value); }
        public bool StartWithLastOpenFiles { get => Get(false); set => Set(value); }
        public string ShowLineNumbers { get => Get("On"); set { Set(value); if (App.VM != null) App.VM.EditorOptions.LineNumbers = Enum.Parse<LineNumbersType>(value); } }
        public bool ShowLog { get => Get(false); set => Set(value); }
        public bool UseModes { get => Get(false); set => Set(value); }
        public bool UseParameters { get => Get(true); set => Set(value); }
        public bool FirstStart { get => Get(true); set => Set(value); }
        public bool ShowOutline { get => Get(true); set => Set(value); }
        public bool ShowProjects { get => Get(true); set => Set(value); }
        public bool ShowProjectPane { get => Get(true); set => Set(value); }
        public bool AutoOpenPDF { get => Get(true); set => Set(value); }
        public bool AutoOpenLOG { get => Get(false); set => Set(value); }
        public bool EvergreenInstalled { get => Get(false); set => Set(value); }
        public bool HelpPDFInInternalViewer { get => Get(false); set => Set(value); }
        public bool AutoOpenLOGOnlyOnError { get => Get(true); set => Set(value); }
        public bool InternalViewer { get => Get(true); set => Set(value); }
        public bool DistributionInstalled { get => Get(false); set => Set(value); }
        public string Wrap { get => Get("On"); set { Set(value); if (App.VM != null) App.VM.EditorOptions.WordWrap = Enum.Parse<WordWrap>(value); } }
        public string NavigationViewPaneMode { get => Get("Auto"); set => Set(value); }
        public string AdditionalParameters { get => Get("--autogenerate --noconsole"); set => Set(value); }
        public string ContextDistributionPath { get => Get(ApplicationData.Current.LocalFolder.Path); set => Set(value); }
        public string TexFilePath { get => Get(""); set => Set(value); }
        public string TexFileFolder { get => Get(""); set => Set(value); }
        public string LastActiveProject { get => Get(""); set => Set(value); }
        public string ContextDownloadLink { get => Get(@"http://lmtx.pragma-ade.nl/install-lmtx/context-mswin.zip"); set => Set(value); }
        public string Theme { get => Get("Default"); set { Set(value); ((AccentColorSetting)Application.Current.Resources["AccentColorSetting"]).Theme = value == "Dark" ? ElementTheme.Dark : (value == "Light" ? ElementTheme.Light : ElementTheme.Default); } }
        public string AccentColor { get => Get("Default"); set { Set(value); } }
        public string TexFileName { get => Get(""); set => Set(value); }
        public string Modes { get => Get(""); set => Set(value); }
        public bool CodeFolding { get => Get(true); set { Set(value); if (App.VM != null) App.VM.EditorOptions.Folding = value; } }
        public bool MiniMap { get => Get(true); set { Set(value); if (App.VM != null) App.VM.EditorOptions.Minimap = new EditorMinimapOptions() { Enabled = value, ShowSlider =  Show.Always, RenderCharacters = true }; ; } }
        public bool Hover { get => Get(true); set { Set(value); if (App.VM != null) App.VM.EditorOptions.Hover = new EditorHoverOptions() { Enabled = value, Delay = 100, Sticky = true }; } }
        public bool SuggestStartStop { get => Get(true); set => Set(value); }
        public bool SuggestPrimitives { get => Get(true); set => Set(value); }
        public bool SuggestFontSwitches { get => Get(true); set => Set(value); }
        public bool SuggestCommands { get => Get(true); set => Set(value); }
        public bool SuggestArguments { get => Get(true); set => Set(value); }
        public string PackageID { get => Get(Package.Current.Id.FamilyName); set => Set(value); }
        public ObservableCollection<Project> ProjectList { get => Get(new ObservableCollection<Project>()); set => Set(value); }
       
        public ObservableCollection<HelpItem> HelpItemList
        {
            get => Get(new ObservableCollection<HelpItem>()
            );
            set => Set(value);
        }

        [JsonIgnore]
        public string[] ShowLineNumberOptions
        {
            get
            {
                return Enum.GetNames<LineNumbersType>();
            }
        }

        [JsonIgnore]
        public string[] ThemeOption
        {
            get
            {
                return Enum.GetNames<ElementTheme>();
            }
        }

        [JsonIgnore]
        public string[] WordWrapOptions
        {
            get
            {
                return Enum.GetNames<WordWrap>();
            }
        }
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