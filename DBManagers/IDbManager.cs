using System;
using System.Collections.Generic;
using System.Text;
using ColdStorageManager.Models;

namespace ColdStorageManager.DBManagers
{
	public interface IDbManager
	{
		// constants for table column types
		public const int TEXT = 0;
		public const int INTEGER = 1;

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
	}
}
