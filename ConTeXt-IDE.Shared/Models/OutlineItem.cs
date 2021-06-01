using ConTeXt_IDE.Helpers;
namespace ConTeXt_IDE.Models
{
    // Class needed in the future to show the document outline (list of sections within the current tex file)
    public class OutlineItem : Bindable
    {
        public string SectionLevel { get => Get<string>(); set => Set(value); }

        public string Title { get => Get<string>(); set => Set(value); }

        public int Row { get => Get<int>(); set => Set(value); }
    }
}
