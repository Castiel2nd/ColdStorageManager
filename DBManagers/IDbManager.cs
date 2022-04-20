using System;
using System.Collections.Generic;
using System.Text;
using ColdStorageManager.Models;

namespace ColdStorageManager.DBManagers
{
	public enum CSMColumnType {NullType, Text, Integer}

	public interface IDbManager
	{
		public const string DbCapturesTableName = "Captures";

		public bool IsConnected { get; }

		public List<Capture> GetCaptures();

		public void SaveCapture(Capture capture);

		// id is contained in Capture instance itself
		public void UpdateCaptureById(Capture capture);

		public void DeleteCapture(Capture capture);

		public bool DeleteAllCaptures();

		public void CloseConnection();

		public bool ConnectAndCreateTable();

		public bool CreateTable(string tableName, List<ColumnInfo> columns);

		public List<string> GetTableNames();

		public ColumnInfo[] GetColumns(string tableName);

		public bool SetTableData(ref TableModel table);

		public bool SetCellData(string tableName, string columnName, int rowId, object data);

		public long GetLastInsertedRowId();

		public object[] GetLastInsertedRow(string tableName, ColumnInfo[] columns);

		public object[] GetRowById(string tableName, ColumnInfo[] columns, long rowId);

		public bool DeleteRowById(string tableName, long rowId);
	}
}
