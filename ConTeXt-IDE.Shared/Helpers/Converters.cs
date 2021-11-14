using ConTeXt_IDE.Models;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Reflection;
using Windows.UI.Text;

namespace ConTeXt_IDE.Helpers
{
    public class StringComparer : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string currenttext = value as string;

            if (string.IsNullOrEmpty(currenttext) | string.IsNullOrEmpty(App.VM.CurrentFileItem.LastSaveFileContent))
                return Visibility.Collapsed;

            return currenttext == App.VM.CurrentFileItem.LastSaveFileContent ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((bool)value).ToString();
        }
    }

    public class ArgumentTypeToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Argument argument)
            {
                string openingdelimiter;
                string closingdelimiter;
                switch (argument.Delimiters)
                {
                    case "braces": openingdelimiter = "{"; closingdelimiter = "}"; break;
                    case "none": openingdelimiter = ""; closingdelimiter = ""; break;
                    default: openingdelimiter = "["; closingdelimiter = "]"; break;
                }

                string argumentcontent = "";
                switch (argument)
                {
                    case Keywords arg: argumentcontent = arg.List == "yes" ? "..., ..." : "..."; break;
                    case Assignments arg: argumentcontent = arg.List == "yes" ? "...=..., ...=..." : "...=..."; break;
                }

                return openingdelimiter + argumentcontent + closingdelimiter;
            }
            else return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return "";
        }
    }

    public class ArgumentOptionalToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string optional)
            {
                return optional == "yes" ? "OPT" : "";
            }
            else return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return "";
        }
    }

    public class ParameterDefaultToDecoration : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string parameterdefault)
            {
                return parameterdefault == "yes" ? TextDecorations.Underline : TextDecorations.None;
            }
            else return TextDecorations.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return "";
        }
    } 
    
    public class ParameterTypeToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string parametertype && value != null)
            {
                if (parametertype.StartsWith("cd:"))
                {
                    string type = parametertype.Remove(0,3);
                    return type.ToUpper();
                }
                else return parametertype;
            }
            else return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return "";
        }
    }

 public class CommandTypeToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string type)
            {
                return type == "environment" ? "start" : "";
            }
            else return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return "";
        }
    }

    public class StringToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string currenttext = value as string;

            if (string.IsNullOrEmpty(currenttext))
                return Visibility.Collapsed;
            else
                return Visibility.Visible;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((bool)value).ToString();
        }
    }

    public class StringToEnum : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Enum.Parse(Assembly.GetExecutingAssembly().GetType(parameter as string), value as string,false);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value.ToString();
        }
    }

    public class EnumToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value as Enum).ToString("g");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            
            return Enum.Parse(Assembly.GetExecutingAssembly().GetType(parameter as string), value as string, false);
        }
    }

    public class StringToThemeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ElementTheme Mode = ElementTheme.Default;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                switch (value.ToString())
                {
                    case "Default": Mode = ElementTheme.Default; break;
                    case "Light": Mode = ElementTheme.Light; break;
                    case "Dark": Mode = ElementTheme.Dark; break;
                    default: Mode = ElementTheme.Default; break;
                }
            }

            return Mode;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((ElementTheme)value).ToString();
        }
    }

    public class ErrorStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility IsVisible = Visibility.Visible;
            if (value != null)
            {
                string str = value.ToString().Trim();
                if (str == "" | str == "?" | str == "0")
                    IsVisible = Visibility.Collapsed;
            }
            else
                IsVisible = Visibility.Collapsed;
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value).ToString();
        }
    }

    public class BoolInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = true;
            if (value is bool)
            {
                invert = !(bool)value;
            }
            return invert;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool invert = true;
            if (value is bool)
            {
                invert = !(bool)value;
            }
            return invert;
        }
    }

    public class LevelToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (int)value == int.Parse(parameter.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool invert = true;
            if (value is bool)
            {
                invert = !(bool)value;
            }
            return invert;
        }
    }

    public class SetRootVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = (FileItem)value;
            return item.Level == 0 && item.FileLanguage == "ConTeXt" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class FileTypeToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((string)value).ToLower() == ((string)parameter).ToLower() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return true;
        }
    }

    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            FontWeight isroot = FontWeights.Normal;
            if (value is bool)
            {
                isroot = (bool)value ? FontWeights.Bold : FontWeights.Normal;
            }
            return isroot;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return false;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility IsVisible = Visibility.Visible;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                IsVisible = (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value) == Visibility.Visible ? true : false;
        }
    }

    public class BoolToInvisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Visibility IsVisible = Visibility.Collapsed;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                IsVisible = (bool)value ? Visibility.Collapsed : Visibility.Visible;
            }
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value) == Visibility.Collapsed ? true : false;
        }
    }

    public class BoolToWidthConverter : IValueConverter
    {
        private GridLength closed = new GridLength(0, GridUnitType.Pixel);
        private GridLength open = new GridLength(1, GridUnitType.Star);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string width)
            {
                open = new GridLength(int.Parse(width), GridUnitType.Pixel);
            }
            GridLength IsVisible = open;
            if (!string.IsNullOrEmpty(value.ToString()))
            {
                IsVisible = (bool)value ? open : closed;
            }
            return IsVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((GridLength)value) == open ? true : false;
        }
    }

    public class VisibilityToCornerRadius : IValueConverter
    {
        private CornerRadius connectedside = new CornerRadius(2);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string side)
            {
                switch (side)
                {
                    case "left": connectedside = new CornerRadius(0, 2, 2, 0); break;
                    case "right": connectedside = new CornerRadius(2, 0, 0, 2); break;
                    case "leftright": connectedside = new CornerRadius(0); break;
                    default: connectedside = new CornerRadius(2); break;
                }
            }
            if (value is Visibility isConnected)
                return isConnected == Visibility.Visible ? connectedside : new CornerRadius(2);
            else
                return connectedside;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((CornerRadius)value) == connectedside ? true : false;
        }
    }

    public class VisibilityToBorderThickness : IValueConverter
    {
        private Thickness connectedside = new Thickness(1);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string side)
            {
                switch (side)
                {
                    case "left": connectedside = new Thickness(0, 1, 1, 1); break;
                    case "right": connectedside = new Thickness(1, 1, 0, 1); break;
                    case "leftright": connectedside = new Thickness(0,1,0,1); break;
                    default: connectedside = new Thickness(1); break;
                }
            }
            if (value is Visibility isConnected)
                return isConnected == Visibility.Visible ? connectedside : new Thickness(1);
            else
                return connectedside;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Thickness)value) == connectedside ? true : false;
        }
    }

    public class ExplorerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FileTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var explorerItem = (FileItem)item;
            return explorerItem.Type == FileItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
        }
    }
}

