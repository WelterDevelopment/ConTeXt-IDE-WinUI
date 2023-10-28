using ConTeXt_IDE.Helpers;

namespace ConTeXt_IDE.Models
{
	public class ContextParameter : Bindable
	{
		public bool IsSelected { get => Get(false); set => Set(value); }

		public string Parameter { get => Get(""); set => Set(value); }

		public string[] Options { get => Get<string[]>(); set => Set(value); }
	}
}