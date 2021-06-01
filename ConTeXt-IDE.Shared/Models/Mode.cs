using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
    public class Mode : Bindable
    {
        public bool IsSelected { get => Get(false); set => Set(value); }

        public string Name { get => Get<string>(); set => Set(value); }
    }
}
