
using ConTeXt_IDE.Helpers;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.Chat;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ConTeXt_IDE.Models
{
    // TODO: TeachingTip provider, Currently not working
    public class HelpItem : Bindable
    {
        public string ID
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public string Title
        {
            get { return Get("Help"); }
            set { Set(value); }
        }

        public string Subtitle
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public string Text
        {
            get { return Get(""); }
            set { Set(value); }
        }

        public bool Shown
        {
            get { return Get(false); }
            set { Set(value);// App.VM.Default?.SaveSettings(); 
            }
        }
    }
}
