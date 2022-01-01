
using ConTeXt_IDE.Models;
using ConTeXt_IDE.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConTeXt_IDE
{
    public sealed partial class SelectTemplate : ContentDialog
    {
        public SelectTemplate()
        {
            this.InitializeComponent();
        }

        public ObservableCollection<TemplateSelection> templateSelections = new ObservableCollection<TemplateSelection>() {
         new TemplateSelection(){ Content = "Hello World (MWE)", Tag = "mwe", IsSelected = false},
         new TemplateSelection(){ Content = "Single File with basic layouting", Tag = "single", IsSelected = true},
          new TemplateSelection(){ Content = "Project structure for a stepped presentation", Tag = "projpres", IsSelected = false},
          new TemplateSelection(){ Content = "Project structure for a thesis", Tag = "projthes", IsSelected = false}
        };

    }
}
