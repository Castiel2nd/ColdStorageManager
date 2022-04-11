using System;
using System.Collections.Generic;
using System.Text;

namespace ColdStorageManager.Models
{
	public class ColumnInfo
	{
		public int Type { get; set; }
		public string Name { get; set; }

		public ColumnInfo(int type, string name)
		{
			Type = type;
			Name = name;
		}
	}
}
