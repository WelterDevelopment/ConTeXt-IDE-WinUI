using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace ConTeXt_IDE.Helpers
{
	public class SyncTeX
	{
		//[DllImport("SyncTeXparser.dll", CallingConvention = CallingConvention.Cdecl)]
		//static extern synctex_scanner_p synctex_scanner_new_with_output_file() { }

		public async Task<bool> ParseFile(StorageFile file)
		{
			try
			{
				FileName = file.Name;
				string content = await FileIO.ReadTextAsync(file);

				MatchCollection inputmatches = Regex.Matches(content, @"Input:(\d+?):(.+\.tex)");
				foreach (Match match in inputmatches)
				{
					int fileid = int.Parse(match.Groups[1].Value);
					string filepath = match.Groups[2].Value;
					if (!string.IsNullOrEmpty(filepath))
						SyncTeXInputFiles.Add(new() { Id = fileid, Name = filepath });
				}

				MatchCollection pagematches = Regex.Matches(content, @"{(\d+?)\n\[((.|\n)*?)\]");
				foreach (Match match in pagematches)
				{
					int pagenumber = int.Parse(match.Groups[1].Value);
					string entries = match.Groups[2].Value;
					if (!string.IsNullOrEmpty(entries))
					{
						MatchCollection entrymatches = Regex.Matches(entries, @"(h)(\d*?),(\d*?):(\d*?),(\d*?):(\d*?),(\d*?),(\d*?)\n");

						foreach (Match entrymatch in entrymatches)
						{
							string type = entrymatch.Groups[1].Value;

							int id = int.Parse(entrymatch.Groups[2].Value);
							int line = int.Parse(entrymatch.Groups[3].Value);
							int xoffset = int.Parse(entrymatch.Groups[4].Value);
							int yoffset = int.Parse(entrymatch.Groups[5].Value);
							int width = int.Parse(entrymatch.Groups[6].Value);
							int height = int.Parse(entrymatch.Groups[7].Value);
							int depth = int.Parse(entrymatch.Groups[8].Value);

							SyncTeXEntries.Add(new() { 
								Page = pagenumber, 
								Type = type, 
								Id = id, 
								Line = line,   
								XOffset = xoffset / 100000 / 0.75,
								YOffset = yoffset / 100000 / 0.75,
								Width = width / 100000 / 0.75,
								Height = height / 100000 / 0.75,
								Depth = depth / 100000 / 0.75,
							});
						}
					}
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		public string FileName = "";
		public List<SyncTeXInputFile> SyncTeXInputFiles { get; set; } = new();
		public List<SyncTeXEntry> SyncTeXEntries { get; set; } = new();
	}

	public class SyncTeXInputFile
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public class SyncTeXEntry
	{
		public string Type { get; set; } = "h";
		public int Id { get; set; }
		public int Page { get; set; }
		public int Line { get; set; }
		public double XOffset { get; set; }
		public double YOffset { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public double Depth { get; set; }
	}

	public class SyncTeXOutputPage
	{
		public int Page { get; set; }
	}


}
