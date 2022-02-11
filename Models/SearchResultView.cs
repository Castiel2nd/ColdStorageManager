using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager.Models
{
	internal class SearchResultView
	{
		public string Name { get; set; }
		public string Path { get; set; }
		public long Size { get; set; }
		public DateTime CreateDT { get; set; }
		public DateTime LastAccessDT { get; set; }
		public DateTime LastModDT { get; set; }

		public SearchResultView(string name, string path, long size = -1, DateTime createDt = default, DateTime lastAccessDt = default, DateTime lastModDt = default)
		{
			Name = name;
			Path = path;
			Size = size;
			CreateDT = createDt;
			LastAccessDT = lastAccessDt;
			LastModDT = lastModDt;
		}
	}
}
