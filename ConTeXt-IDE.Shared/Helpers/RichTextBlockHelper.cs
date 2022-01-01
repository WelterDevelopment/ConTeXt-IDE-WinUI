using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;

namespace ConTeXt_IDE.Helpers
{
    public class RichTextBlockHelper : DependencyObject
    {
        // Using a DependencyProperty as the backing store for Text.
        //This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BlocksProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string),
            typeof(RichTextBlockHelper),
            new PropertyMetadata(String.Empty, OnTextChanged));

        public static int logline = 0;

        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(BlocksProperty);
        }

        public static Paragraph LOG(string log)
        {
            logline++;
            Paragraph paragraph = new Paragraph();
            Run run1 = new Run
            {
                Text = $"{string.Format("{0,3:###}", logline)} [{DateTime.Now.ToString("HH:mm:ss")}] : "
            };
            //var DefaultTheme = new Windows.UI.ViewManagement.UISettings();
            //Color ForeGroundColor = (Color)Application.Current.Resources["ForegroundLightColor"];
            //var lightbrush = DefaultTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
            //byte max = 255;
            //run1.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(max - lightbrush.R), (byte)(max - lightbrush.G), (byte)(max - lightbrush.B)));
            //run1.Foreground = new SolidColorBrush(ForeGroundColor);
            Run run2 = new Run
            {
                Text = log
            };
            paragraph.Inlines.Add(run1);
            paragraph.Inlines.Add(run2);
            //Log.Blocks.Add(paragraph);
            //Blocks.Add(paragraph);
            return paragraph;
            //Log.UpdateLayout();
            //logscroll.UpdateLayout();
            //logscroll.ChangeView(0, logscroll.ScrollableHeight, 1);
        }

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(BlocksProperty, value);
        }
        private static T FindParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            T parent = VisualTreeHelper.GetParent(child) as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parent);
        }

        private static void OnTextChanged(DependencyObject sender,
                    DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (sender is RichTextBlock control)
                {
                    //control.Blocks.Clear();
                    var value = e.NewValue as string;

                    control.Blocks.Add(LOG(value));
                    control.UpdateLayout();

                    var logscroll = (ScrollViewer)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(control))));
                    logscroll.UpdateLayout();
                    logscroll.ChangeView(0, logscroll.ScrollableHeight, 1);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
