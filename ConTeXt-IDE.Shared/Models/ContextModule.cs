using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
    public enum ContextModuleType { TDSArchive, Archive, GitHub }

    public class ContextModule : Bindable
    {
        public bool IsInstalled { get => Get(false); set => Set(value); }

        public string Name { get => Get(""); set => Set(value); }

        public string Description { get => Get(""); set => Set(value); }

        public string ArchiveFolderPath { get => Get(""); set => Set(value); }

        public string URL { get => Get<string>(); set => Set(value); }

        public ContextModuleType Type { get => Get(ContextModuleType.Archive); set => Set(value); }
    }
}
