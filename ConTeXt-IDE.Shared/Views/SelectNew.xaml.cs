
using ConTeXt_IDE.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConTeXt_IDE
{
    public sealed partial class SelectNew : ContentDialog
    {
        public SelectNew()
        {
            this.InitializeComponent();
        }


        public ObservableCollection<TemplateSelection> templateSelections = new ObservableCollection<TemplateSelection>() {
         new TemplateSelection(){ Content = "Empty and/or existing project folder", Tag = "empty", IsSelected = false},
         new TemplateSelection(){ Content = "New Project folder with template", Tag = "template", IsSelected = false},
        };
    
    }
}
