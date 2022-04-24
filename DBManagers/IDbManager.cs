using System;
using System.Collections.Generic;
using System.Text;
using ColdStorageManager.Models;

namespace ColdStorageManager.DBManagers
{
	public enum CSMColumnType {Text, Integer, NullType}

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

		public bool SetCellData(string tableName, string columnName, object rowId, object data);

		public ulong GetLastInsertedRowId();

		public object[] GetLastInsertedRow(string tableName, ColumnInfo[] columns);

		public object[] GetRowById(string tableName, ColumnInfo[] columns, object rowId);

		public bool DeleteRowById(string tableName, object rowId);

		public bool DeleteTable(string tableName);

		//checks if the provided data is a compatible value for the column type.
		//since for different DbManagers the column types might mean different thing, this has to be checked by the Manager.
		public bool TryParse(CSMColumnType columnType, object data);
	}
}
