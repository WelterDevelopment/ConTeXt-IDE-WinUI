using ConTeXt_IDE.Helpers;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Graphics;

namespace ConTeXt_IDE.Shared.Models
{
 public	class WindowSetting : Bindable
	{
		public RectInt32 LastSize
		{
			get => Get(new RectInt32(24, 24, 1600, 800));
			set
			{
				value.X = Math.Max(0, value.X); // Ensure that the Window is actually visible
				value.Y = Math.Max(0, value.Y);
				value.Width = Math.Max(200, value.Width);// Ensure that the Window starts with a reasonable minimum size
				value.Height = Math.Max(200, value.Height);
				Set(value);
			}
		}

		public AppWindowPresenterKind LastPresenter { get => Get(AppWindowPresenterKind.Default); set => Set(value); }
		public bool IsMaximized { get => Get(false); set => Set(value); }
	}
}
