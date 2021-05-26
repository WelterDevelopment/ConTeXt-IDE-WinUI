using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
    public enum ContextModuleType { TDSArchive, Archive, GitHub }
    public class ContextModule : Bindable
    {
        public bool IsInstalled
        {
            get { return Get(false); }
            set { Set(value); }
        }

        public string Name
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public string Description
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public string ArchiveFolderPath
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public string URL
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public ContextModuleType Type
        {
            get { return Get( ContextModuleType.Archive); }
            set { Set(value); }
        }
    }
}
