using ConTeXt_IDE.Helpers;
namespace ConTeXt_IDE.Models
{
    // Class needed in the future to show the document outline (list of sections within the current tex file)
    public class OutlineItem : Bindable
    {
        public int ID { get => Get(0); set => Set(value); }
        public int SectionLevel { get => Get(0); set => Set(value); }
        public string SectionType { get => Get<string>(); set => Set(value); }

        public string Title { get => Get<string>(); set => Set(value); }

        public int Row { get => Get(0); set => Set(value); }
    }
}
