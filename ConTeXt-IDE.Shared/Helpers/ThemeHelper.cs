using ConTeXt_IDE.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

namespace ConTeXt_IDE.Helpers
{
	public class AccentColor
	{
		public AccentColor(string name, Color color)
		{
			Name = name;
			Color = color;
		}
		public string Name { get; set; }
		public Color Color { get; set; }
	}

	public class AccentColorSetting : Bindable
	{
		public AccentColorSetting()
		{
			AccentColor = (new Windows.UI.ViewManagement.UISettings()).GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
			Theme = ElementTheme.Default;
		}

		public ElementTheme Theme
		{
			get => Get<ElementTheme>();
			set
			{
				Set(value);
				AccentColor = AccentColor;
			}
		}

		public Color AccentColor
		{
			get => Get<Color>();
			set
			{
				Set(value);

				

				float HighFactor = 1;
				float LowFactor = -1;
				switch (Theme)
				{
					case ElementTheme.Dark:
						HighFactor = 1;
						LowFactor = -1;
						break;
					case ElementTheme.Light:
						HighFactor = -1;
						LowFactor = 1;
						break;
					case ElementTheme.Default:
						var uiSettings = new Windows.UI.ViewManagement.UISettings();
						var defaultthemecolor = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
						if (defaultthemecolor == Colors.Black)
						{
							HighFactor = 1;
							LowFactor = -1;
						}
						else
						{
							HighFactor = -1;
							LowFactor = 1;
						}
						break;
				}



				AccentColorHigh = ChangeColorBrightness(value, HighFactor * 0.2f);
				AccentColorLow = ChangeColorBrightness(value, LowFactor * 0.4f);
				//AccentColorLowLow = ReduceColorSaturation(ChangeColorBrightness(value, LowFactor * 0.4f), 0.9f);
				AccentColorLowLow = ChangeColorBrightness(value, LowFactor * (LowFactor < 0 ? 0.75f : 0.8f));
				AccentColorLowLowLow = ReduceColorSaturation(ChangeColorBrightness(value, LowFactor * 0.6f), 0.9f);
				AccentColorLowLowLowLow = ReduceColorSaturation(ChangeColorBrightness(value, LowFactor * 0.8f), 0.9f);

				// Application.Current.Resources["SystemAccentColor"] = value;
			}
		}

		public Color AccentColorHigh { get => Get<Color>(); set => Set(value); }
		public SolidColorBrush AccentBrushLow { get => Get<SolidColorBrush>(); set => Set(value); }
		public Color AccentColorLow { get => Get<Color>(); set { Set(value); AccentBrushLow = new(value); } }
		public Color AccentColorLowLow { get => Get<Color>(); set => Set(value); }
		public Color AccentColorLowLowLow { get => Get<Color>(); set => Set(value); }
		public Color AccentColorLowLowLowLow { get => Get<Color>(); set => Set(value); }

		public static Color ChangeColorBrightness(Color color, float correctionFactor)
		{
			float red = color.R;
			float green = color.G;
			float blue = color.B;

			if (correctionFactor < 0)
			{
				correctionFactor = 1 + correctionFactor;
				red *= correctionFactor;
				green *= correctionFactor;
				blue *= correctionFactor;
			}
			else
			{
				red = (255 - red) * correctionFactor + red;
				green = (255 - green) * correctionFactor + green;
				blue = (255 - blue) * correctionFactor + blue;
			}

			return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
		}

		public static Color ReduceColorSaturation(Color color, float correctionFactor)
		{
			float red = color.R;
			float green = color.G;
			float blue = color.B;

			float L = 0.3f * red + 0.6f * green + 0.1f * blue;
			red = red + correctionFactor * (L - red);
			green = green + correctionFactor * (L - green);
			blue = blue + correctionFactor * (L - blue);

			return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
		}
	}
}
