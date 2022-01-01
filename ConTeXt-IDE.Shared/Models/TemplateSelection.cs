using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
    // Template projects the user can choose from when he adds a project folder
    public class TemplateSelection : Bindable
    {
        public string Content { get => Get<string>(); set => Set(value); }

        public bool IsSelected { get => Get<bool>(); set => Set(value); }

        public string Tag { get => Get<string>(); set => Set(value); }
    }
}
