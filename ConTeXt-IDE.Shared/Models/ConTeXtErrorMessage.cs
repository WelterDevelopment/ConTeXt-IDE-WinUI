

using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
    public class ConTeXtErrorMessage : Bindable
    {
        public int errortype
        {
            get { return Get<int>(0); }
            set { Set(value); }
        }

        public string filename
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string lastcontext
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string lastluaerror
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string lasttexerror
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string lasttexhelp
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public int linenumber
        {
            get { return Get(0); }
            set { Set(value); }
        }

        public int luaerrorline
        {
            get { return Get(0); }
            set { Set(value); }
        }

        public int offset
        {
            get { return Get(0); }
            set { Set(value); }
        }

        public int skiplinenumber
        {
            get { return Get(0); }
            set { Set(value); }
        }
    }
}
