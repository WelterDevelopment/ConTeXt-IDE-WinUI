

using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
    public class ConTeXtErrorMessage : Bindable
    {
        public int errortype { get => Get(0); set => Set(value); }

        public string filename { get => Get<string>(); set => Set(value); }

        public string lastcontext { get => Get<string>(); set => Set(value); }

        public string lastluaerror { get => Get<string>(); set => Set(value); }

        public string lasttexerror { get => Get<string>(); set => Set(value); }

        public string lasttexhelp { get => Get<string>(); set => Set(value); }

        public int linenumber { get => Get(1); set => Set(value); }

        public int luaerrorline { get => Get(0); set => Set(value); }

        public int offset { get => Get(0); set => Set(value); }

        public int skiplinenumber { get => Get(0); set => Set(value); }
    }
}
