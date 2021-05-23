using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
    public class Mode : Bindable
    {
        public bool IsSelected
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
    }
}
