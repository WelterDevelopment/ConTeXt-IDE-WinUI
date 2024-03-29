﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConTeXt_IDE.Helpers
{
 // Helper class to get the INotifyPropertyChanged models as clean an simple as possible
 public class Bindable : INotifyPropertyChanged
 {
  private Dictionary<string, object> _properties = new Dictionary<string, object>();

  public event PropertyChangedEventHandler PropertyChanged;

  protected T Get<T>(T defaultVal = default, [CallerMemberName] string name = null)
  {
   if (!_properties.TryGetValue(name, out object value))
   {
    value = _properties[name] = defaultVal;
   }
   return (T)value;
  }

  protected void Set<T>(T value, [CallerMemberName] string name = null)
  {
   //if (name != "Blocks")
    if (Equals(value, Get<T>(value, name)))
     return;
   _properties[name] = value;

   //if (name != "FileContent")
    OnPropertyChanged(name);
  }

  protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
  {
   PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
 }
}
