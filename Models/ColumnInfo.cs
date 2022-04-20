using System;
using System.Collections.Generic;
using System.Text;
using ColdStorageManager.DBManagers;

namespace ColdStorageManager.Models
{
	public class ColumnInfo
	{
		public CSMColumnType Type { get; set; }
		public string Name { get; set; }

		public bool IsTextEditable { get; set; } = true;

		public ColumnInfo(){}

		public ColumnInfo(CSMColumnType type, string name)
		{
			Type = type;
			Name = name;
		}

		public ColumnInfo(CSMColumnType type, string name, bool isTextEditable)
		{
			Type = type;
			Name = name;
			IsTextEditable = isTextEditable;
		}
	}
}
