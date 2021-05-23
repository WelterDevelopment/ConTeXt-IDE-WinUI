using ConTeXt_IDE.Helpers;
namespace ConTeXt_IDE.Models
{
    // Class needed in the future to show the document outline (list of sections within the current tex file)
    public class OutlineItem : Bindable
    {
        public string SectionLevel
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string Title
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public int Row
        {
            get { return Get<int>(); }
            set { Set(value); }
        }
    }
}
