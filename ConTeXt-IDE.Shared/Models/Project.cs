
using ConTeXt_IDE.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace ConTeXt_IDE.Models
{
	public class PDFViewer : Bindable
	{
		public PDFViewer(string name = "", string path = "")
		{
			Name = name;
			Path = path;
		}
		public string Name { get => Get(""); set => Set(value); }
		public string Path { get => Get(""); set => Set(value); }
	}
}
