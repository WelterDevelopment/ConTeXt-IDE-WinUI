using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
    public class Helpfile : Bindable
    {
        public string FileName { get => Get<string>(); set => Set(value); }

        public string FriendlyName { get => Get<string>(); set => Set(value); }

        public string Path { get => Get<string>(); set => Set(value); }
    }
}
