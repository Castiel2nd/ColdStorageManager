using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ColdStorageManager.Models
{
	public class TableModel
	{
		public string Name { get; set; }

		public ColumnInfo[] Columns { get; set; }

		public ObservableCollection<object[]> Data { get; set; }

		public ColumnInfo GetColumnByName(string columnName)
		{
			foreach (var columnInfo in Columns)
			{
				if (columnInfo.Name.Equals(columnName))
				{
					return columnInfo;
				}
			}

			return null;
		}

		public int GetColumnIndexByName(string columnName)
		{
			for (int i = 0; i < Columns.Length; i++)
			{
				if (Columns[i].Name.Equals(columnName))
				{
					return i;
				}
			}

			return -1;
		}

		//replaces the first occurrence of the specified row with another one
		// returns false if the row could not be found, and does not alter the data
		public bool ReplaceRow(object[] rowToReplace, object[] newRow)
		{
			int index = Data.IndexOf(rowToReplace);

			if (index == -1)
			{
				return false;
			}

			Data.RemoveAt(index);
			Data.Insert(index, newRow);

			return true;
		}

		public TableModel(string name, ColumnInfo[] columns)
		{
			Name = name;
			Columns = columns;
		}

		public TableModel(string name, ColumnInfo[] columns, ObservableCollection<object[]> data)
		{
			Name = name;
			Columns = columns;
			Data = data;
		}
	}
}
